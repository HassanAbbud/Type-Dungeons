using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wraps WordGenerator with weighted difficulty blending.
/// Instead of hard cutoffs (level 3 = all medium), this blends tiers:
///   e.g. level 4 → 40% easy + 50% medium + 10% hard
///
/// SETUP: Attach to same GameObject as WordGenerator (or any GO in scene).
///        Drag WordGenerator into the Inspector slot.
///        GameManager.RequestNextWord() routes here automatically.
/// </summary>
public class WordDifficultyScaler : MonoBehaviour
{
    #region Singleton
    public static WordDifficultyScaler Instance { get; private set; }

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

    [Header("Reference — drag WordGenerator here")]
    [SerializeField] private WordGenerator wordGenerator;

    [Header("Difficulty Profiles")]
    [Tooltip("How word difficulty blends at each level range. First matching profile wins.")]
    [SerializeField]
    private List<DifficultyProfile> profiles = new List<DifficultyProfile>()
    {
        new DifficultyProfile { profileName = "Tutorial", fromLevel = 1,  toLevel = 2,  easyWeight = 1.0f, mediumWeight = 0.0f, hardWeight = 0.0f },
        new DifficultyProfile { profileName = "Early",    fromLevel = 3,  toLevel = 4,  easyWeight = 0.4f, mediumWeight = 0.5f, hardWeight = 0.1f },
        new DifficultyProfile { profileName = "Mid",      fromLevel = 5,  toLevel = 7,  easyWeight = 0.1f, mediumWeight = 0.5f, hardWeight = 0.4f },
        new DifficultyProfile { profileName = "Late",     fromLevel = 8,  toLevel = 10, easyWeight = 0.0f, mediumWeight = 0.3f, hardWeight = 0.7f },
        new DifficultyProfile { profileName = "Endgame",  fromLevel = 11, toLevel = 99, easyWeight = 0.0f, mediumWeight = 0.1f, hardWeight = 0.9f },
    };

    [Header("Fake Levels for WordGenerator")]
    [Tooltip("Must match WordGenerator's mediumLevelStart / hardLevelStart thresholds")]
    [SerializeField] private int fakeLevelEasy = 1;
    [SerializeField] private int fakeLevelMedium = 4;
    [SerializeField] private int fakeLevelHard = 7;

    [Header("Anti-Repeat")]
    [Tooltip("Track last N words to avoid consecutive repeats")]
    [SerializeField] private int recentWordMemory = 15;

    #endregion

    #region Data Structures

    [Serializable]
    public class DifficultyProfile
    {
        public string profileName;
        [Range(1, 99)] public int fromLevel = 1;
        [Range(1, 99)] public int toLevel = 5;
        [Range(0f, 1f)] public float easyWeight = 0.5f;
        [Range(0f, 1f)] public float mediumWeight = 0.3f;
        [Range(0f, 1f)] public float hardWeight = 0.2f;
    }

    private enum WordTier { Easy, Medium, Hard }

    #endregion

    #region Runtime State
    private int currentLevel = 1;
    private Queue<string> recentWords = new Queue<string>();
    private bool isSubscribed = false;

    public event Action<string> OnDifficultyProfileChanged;
    #endregion

    #region Lifecycle

    private void Start()
    {
        TrySubscribe();
        if (!isSubscribed)
            StartCoroutine(RetrySubscribe());
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelChanged += HandleLevelChanged;
            isSubscribed = true;
        }
    }

    private IEnumerator RetrySubscribe()
    {
        float timeout = 2f;
        while (!isSubscribed && timeout > 0f)
        {
            yield return null;
            timeout -= Time.unscaledDeltaTime;
            TrySubscribe();
        }

        if (!isSubscribed)
            Debug.LogError("[WordDifficultyScaler] GameManager.Instance never became available!");
    }

    private void OnDestroy()
    {
        if (isSubscribed && GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= HandleLevelChanged;

        if (Instance == this) Instance = null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get a word with blended difficulty based on current level.
    /// Called by GameManager.RequestNextWord().
    /// </summary>
    public string GetWord()
    {
        if (wordGenerator == null)
        {
            Debug.LogError("[WordDifficultyScaler] WordGenerator reference not set in Inspector!");
            return "error";
        }

        // Wait for JSON to load before serving words
        if (!wordGenerator.IsLoaded)
        {
            Debug.LogWarning("[WordDifficultyScaler] WordGenerator still loading JSON...");
            return "loading";
        }

        WordTier tier = RollTier();
        string word = GetWordFromTier(tier);

        // Guard: WordGenerator returns "" if a bank is empty
        if (string.IsNullOrEmpty(word))
        {
            word = TryFallbackTiers(tier);
        }

        // Anti-repeat
        int attempts = 0;
        while (recentWords.Contains(word) && attempts < 10)
        {
            word = GetWordFromTier(tier);
            if (string.IsNullOrEmpty(word)) break;
            attempts++;
        }

        if (!string.IsNullOrEmpty(word))
            TrackWord(word);

        return string.IsNullOrEmpty(word) ? "type" : word;
    }

    /// <summary>Get multiple unique words at once (for spawning a wave of enemies).</summary>
    public List<string> GetWords(int count)
    {
        var words = new List<string>(count);
        var usedThisBatch = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string word = GetWord();
            int retries = 0;
            while (usedThisBatch.Contains(word) && retries < 10)
            {
                word = GetWord();
                retries++;
            }
            usedThisBatch.Add(word);
            words.Add(word);
        }
        return words;
    }

    /// <summary>Force a specific tier (e.g. "Hard" for boss, "Easy" for tutorial).</summary>
    public string GetWordFromTierName(string tierName)
    {
        if (Enum.TryParse(tierName, true, out WordTier tier))
        {
            string word = GetWordFromTier(tier);
            if (!string.IsNullOrEmpty(word))
            {
                TrackWord(word);
                return word;
            }
        }
        return GetWord();
    }

    /// <summary>Get current active profile name for UI display.</summary>
    public string GetCurrentProfileName()
    {
        var profile = FindProfile(currentLevel);
        return profile?.profileName ?? "Default";
    }

    #endregion

    #region Internals

    private void HandleLevelChanged(int newLevel)
    {
        int oldLevel = currentLevel;
        currentLevel = newLevel;
        recentWords.Clear();

        var oldProfile = FindProfile(oldLevel);
        var newProfile = FindProfile(newLevel);

       // Fire if profile CHANGED, OR if this is the first time (oldLevel was 0 initially), nmendo16
        if (oldProfile?.profileName != newProfile?.profileName || newLevel == 1)
        {
            OnDifficultyProfileChanged?.Invoke(newProfile?.profileName ?? "Default");
            Debug.Log($"[WordDifficultyScaler] Profile changed/initialized to: {newProfile?.profileName}");
        }
    }

    private DifficultyProfile FindProfile(int level)
    {
        foreach (var profile in profiles)
        {
            if (level >= profile.fromLevel && level <= profile.toLevel)
                return profile;
        }
        return profiles.Count > 0 ? profiles[profiles.Count - 1] : null;
    }

    private WordTier RollTier()
    {
        var profile = FindProfile(currentLevel);
        if (profile == null) return WordTier.Easy;

        float total = profile.easyWeight + profile.mediumWeight + profile.hardWeight;
        if (total <= 0f) return WordTier.Easy;

        float roll = UnityEngine.Random.Range(0f, total);
        if (roll < profile.easyWeight) return WordTier.Easy;
        if (roll < profile.easyWeight + profile.mediumWeight) return WordTier.Medium;
        return WordTier.Hard;
    }

    private string GetWordFromTier(WordTier tier)
    {
        int fakeLevel = tier switch
        {
            WordTier.Easy => fakeLevelEasy,
            WordTier.Medium => fakeLevelMedium,
            WordTier.Hard => fakeLevelHard,
            _ => fakeLevelEasy
        };
        return wordGenerator.GetRandomWord(fakeLevel);
    }

    private string TryFallbackTiers(WordTier failedTier)
    {
        WordTier[] allTiers = { WordTier.Easy, WordTier.Medium, WordTier.Hard };
        foreach (var tier in allTiers)
        {
            if (tier == failedTier) continue;
            string word = GetWordFromTier(tier);
            if (!string.IsNullOrEmpty(word)) return word;
        }
        return "";
    }

    private void TrackWord(string word)
    {
        recentWords.Enqueue(word);
        if (recentWords.Count > recentWordMemory)
            recentWords.Dequeue();
    }

    #endregion
}