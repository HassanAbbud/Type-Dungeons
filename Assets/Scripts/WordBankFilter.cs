using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WordBankFilter : MonoBehaviour
{
    #region Singleton
    public static WordBankFilter Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Inspector

    [Header("Filter Mode")]
    [Tooltip("Current content mode. Children mode is stricter.")]
    [SerializeField] private ContentMode currentMode = ContentMode.Teen;

    [Header("Word Length Rules")]
    [SerializeField] private int minWordLength = 2;
    [SerializeField] private int maxWordLengthTeen = 20;
    [SerializeField] private int maxWordLengthChildren = 8;

    [Header("Blocked Words — applies to ALL modes")]
    [Tooltip("Exact words always blocked regardless of mode")]
    [SerializeField]
    private List<string> globalBlockedWords = new List<string>
    {
        // profanity
        "fuck", "shit", "damn", "hell", "ass", "bitch", "crap",
        "dick", "piss", "slut", "whore", "bastard", "cunt",
        // slurs (abbreviated — extend as needed)
        "nigger", "faggot", "retard", "spic", "kike",
        // violence 
        "kill", "murder", "rape", "suicide", "torture",
        // drugs
        "cocaine", "heroin", "meth", "weed", "drug",
        // sexual
        "sex", "porn", "nude", "boob", "penis", "vagina"
    };

    [Header("Additional Blocked — Children Mode Only")]
    [Tooltip("Extra words blocked only in children mode")]
    [SerializeField]
    private List<string> childrenExtraBlocked = new List<string>
    {
        // mild words OK for teens but not children
        "death", "dead", "die", "blood", "evil", "demon",
        "zombie", "skull", "grave", "corpse", "ghost",
        "poison", "dagger", "slay", "destroy", "doom",
        "curse", "venom", "wrath", "fear", "horror",
        "witch", "devil", "beast", "plague", "tomb"
    };

    [Header("Blocked Substrings")]
    [Tooltip("If any word CONTAINS these substrings, it gets filtered out")]
    [SerializeField]
    private List<string> blockedSubstrings = new List<string>
    {
        "fuck", "shit", "ass", "dick", "cock", "cunt",
        "nigg", "fag", "slut", "whore"
    };

    #endregion

    #region Types

    public enum ContentMode
    {
        Teen,       // moderate filter — allows medieval/combat themed words
        Children    // strict filter — simple, safe vocabulary only
    }

    #endregion

    #region Runtime State

    // HashSets for O(1) lookup during filtering
    private HashSet<string> blockedExactSet = new HashSet<string>();
    private HashSet<string> childrenExtraSet = new HashSet<string>();
    private int filteredCount = 0;

    #endregion

    #region Lifecycle

    private void Start()
    {
        RebuildBlockedSets();
    }

    #endregion

    #region Public API

    
    /// Filter a list of words. Returns only words that pass the content filter.
    public List<string> FilterWords(List<string> words)
    {
        if (words == null || words.Count == 0)
            return new List<string>();

        filteredCount = 0;
        int maxLen = currentMode == ContentMode.Children
            ? maxWordLengthChildren
            : maxWordLengthTeen;

        var filtered = new List<string>(words.Count);

        foreach (string rawWord in words)
        {
            string word = rawWord.ToLower().Trim();

            if (!IsValidWord(word, maxLen))
            {
                filteredCount++;
                continue;
            }

            filtered.Add(word);
        }

        if (filteredCount > 0)
            Debug.Log($"[WordBankFilter] Filtered {filteredCount} words in {currentMode} mode. {filtered.Count} words passed.");

        return filtered.Distinct().ToList();
    }

    /// Check a single word against the filter. 
    public bool IsWordAllowed(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        int maxLen = currentMode == ContentMode.Children
            ? maxWordLengthChildren
            : maxWordLengthTeen;
        return IsValidWord(word.ToLower().Trim(), maxLen);
    }

    /// Switch content mode. Called when player selects Teen/Children in category selector.
    public void SetMode(ContentMode mode)
    {
        currentMode = mode;
        RebuildBlockedSets();
        Debug.Log($"[WordBankFilter] Mode set to: {mode}");
    }

    /// Switch mode by string name. Convenience for UI dropdown.
    public void SetMode(string modeName)
    {
        if (modeName.ToLower().Contains("child"))
            SetMode(ContentMode.Children);
        else
            SetMode(ContentMode.Teen);
    }

    /// Get current mode.
    public ContentMode GetCurrentMode() => currentMode;

    /// Add a blocked word at runtime (e.g. from facilitator dashboard).
    public void AddBlockedWord(string word)
    {
        string lower = word.ToLower().Trim();
        if (!globalBlockedWords.Contains(lower))
        {
            globalBlockedWords.Add(lower);
            blockedExactSet.Add(lower);
        }
    }

    /// Add a blocked substring at runtime.
    public void AddBlockedSubstring(string substring)
    {
        string lower = substring.ToLower().Trim();
        if (!blockedSubstrings.Contains(lower))
            blockedSubstrings.Add(lower);
    }

    /// Get filter stats for debug/UI.
    public (int globalBlocked, int childrenExtra, int substrings, ContentMode mode) GetFilterStats()
    {
        return (globalBlockedWords.Count, childrenExtraBlocked.Count,
                blockedSubstrings.Count, currentMode);
    }

    #endregion

    #region Internal Filtering Logic

    private bool IsValidWord(string word, int maxLen)
    {
        // 1. Length check
        if (word.Length < minWordLength || word.Length > maxLen)
            return false;

        // 2. Letters only (no numbers, symbols, spaces)
        if (!IsLettersOnly(word))
            return false;

        // 3. Exact match against blocked list
        if (blockedExactSet.Contains(word))
            return false;

        // 4. Children mode: check extra blocked words
        if (currentMode == ContentMode.Children && childrenExtraSet.Contains(word))
            return false;

        // 5. Substring check — catches hidden profanity
        if (ContainsBlockedSubstring(word))
            return false;

        return true;
    }

    private bool IsLettersOnly(string word)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (!char.IsLetter(word[i]))
                return false;
        }
        return true;
    }

    private bool ContainsBlockedSubstring(string word)
    {
        // Only check if the word is longer than the substring
        // to avoid filtering out short words that happen to match
        foreach (string sub in blockedSubstrings)
        {
            if (word.Length > sub.Length && word.Contains(sub))
                return true;
        }
        return false;
    }

    private void RebuildBlockedSets()
    {
        blockedExactSet.Clear();
        foreach (string w in globalBlockedWords)
            blockedExactSet.Add(w.ToLower().Trim());

        childrenExtraSet.Clear();
        foreach (string w in childrenExtraBlocked)
            childrenExtraSet.Add(w.ToLower().Trim());
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}