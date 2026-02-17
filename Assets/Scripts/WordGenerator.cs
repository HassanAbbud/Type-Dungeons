using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Loads word banks from JSON files in StreamingAssets/WordBanks/.
/// Keeps the same public API: GetRandomWord(int currentLevel)
/// so WordDifficultyScaler and everything else works unchanged.
///
/// JSON files go in: Assets/StreamingAssets/WordBanks/
///   - teen_mode.json    (default)
///   - children_mode.json
///
/// Call SwitchWordBank("children_mode") to swap at runtime
/// (for the category selector UI in Iteration 2).
/// </summary>
public class WordGenerator : MonoBehaviour
{
    #region JSON Data Structures
    [Serializable]
    private class WordBankJson
    {
        public string bankName;
        public string version;
        public string description;
        public WordTierJson[] tiers;
        public string[] blockedWords;
    }

    [Serializable]
    private class WordTierJson
    {
        public string tierName;
        public int difficulty;
        public string[] words;
    }
    #endregion

    #region Inspector Settings
    [Header("Word Bank Settings")]
    [Tooltip("JSON filename without extension (must be in StreamingAssets/WordBanks/)")]
    [SerializeField] private string defaultWordBank = "teen_mode";

    [Header("Difficulty Thresholds")]
    [SerializeField] private int mediumLevelStart = 3;
    [SerializeField] private int hardLevelStart = 6;

    [Header("Fallback Words (used if JSON fails to load)")]
    [SerializeField] private List<string> fallbackEasy = new List<string> { "the", "and", "for", "run", "cat", "dog" };
    [SerializeField] private List<string> fallbackMedium = new List<string> { "sword", "armor", "quest", "guard", "flame" };
    [SerializeField] private List<string> fallbackHard = new List<string> { "fortress", "guardian", "prophecy", "labyrinth" };
    #endregion

    #region Runtime State
    private List<string> easyWords = new List<string>();
    private List<string> mediumWords = new List<string>();
    private List<string> hardWords = new List<string>();
    private HashSet<string> blockedWords = new HashSet<string>();

    public bool IsLoaded { get; private set; } = false;
    public string CurrentBankName { get; private set; } = "";

    public event Action<string> OnWordBankLoaded;  // bank name
    public event Action<string> OnWordBankFailed;  // error message
    #endregion

    #region Lifecycle

    private void Start()
    {
        StartCoroutine(LoadWordBank(defaultWordBank));
    }

    #endregion

    #region Public API — same signature as before

    /// <summary>
    /// Same API as before. WordDifficultyScaler calls this with fake levels.
    /// Returns a random word from the tier matching the given level.
    /// </summary>
    public string GetRandomWord(int currentLevel)
    {
        List<string> selectedBank = GetWordBank(currentLevel);

        if (selectedBank == null || selectedBank.Count == 0)
        {
            Debug.LogWarning($"[WordGenerator] Word bank empty for level {currentLevel}!");
            return "";
        }

        int index = UnityEngine.Random.Range(0, selectedBank.Count);
        return selectedBank[index];
    }

    /// <summary>
    /// Switch word banks at runtime.
    /// Call this from the category selector UI (children vs teen mode).
    /// </summary>
    /// <param name="bankFileName">JSON filename without extension, e.g. "children_mode"</param>
    public void SwitchWordBank(string bankFileName)
    {
        StartCoroutine(LoadWordBank(bankFileName));
    }

    /// <summary>Get word count per tier for UI/debug display.</summary>
    public (int easy, int medium, int hard) GetWordCounts()
    {
        return (easyWords.Count, mediumWords.Count, hardWords.Count);
    }

    /// <summary>Check if a specific word exists in any tier.</summary>
    public bool ContainsWord(string word)
    {
        string lower = word.ToLower().Trim();
        return easyWords.Contains(lower) || mediumWords.Contains(lower) || hardWords.Contains(lower);
    }

    #endregion

    #region Word Bank Selection (unchanged logic)

    private List<string> GetWordBank(int currentLevel)
    {
        if (currentLevel < mediumLevelStart)
            return easyWords;

        if (currentLevel < hardLevelStart)
            return mediumWords;

        return hardWords;
    }

    #endregion

    #region JSON Loading

    private IEnumerator LoadWordBank(string bankFileName)
    {
        IsLoaded = false;
        string path = Path.Combine(Application.streamingAssetsPath, "WordBanks", bankFileName + ".json");

        // UnityWebRequest works on ALL platforms including WebGL
        // (File.ReadAllText does NOT work on WebGL)
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"Failed to load word bank '{bankFileName}': {request.error}";
                Debug.LogWarning($"[WordGenerator] {error}. Using fallback words.");
                UseFallbackWords();
                OnWordBankFailed?.Invoke(error);
                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                ParseJsonWordBank(json);
                CurrentBankName = bankFileName;
                IsLoaded = true;

                var counts = GetWordCounts();
                Debug.Log($"[WordGenerator] Loaded '{bankFileName}': {counts.easy} easy, {counts.medium} medium, {counts.hard} hard words.");
                OnWordBankLoaded?.Invoke(bankFileName);
            }
            catch (Exception e)
            {
                string error = $"Failed to parse '{bankFileName}': {e.Message}";
                Debug.LogError($"[WordGenerator] {error}");
                UseFallbackWords();
                OnWordBankFailed?.Invoke(error);
            }
        }
    }

    private void ParseJsonWordBank(string json)
    {
        WordBankJson data = JsonUtility.FromJson<WordBankJson>(json);

        // Build blocked words set
        blockedWords.Clear();
        if (data.blockedWords != null)
        {
            foreach (string word in data.blockedWords)
                blockedWords.Add(word.ToLower().Trim());
        }

        // Clear existing lists
        easyWords.Clear();
        mediumWords.Clear();
        hardWords.Clear();

        // Parse tiers — map by difficulty value
        if (data.tiers == null || data.tiers.Length == 0)
        {
            Debug.LogWarning("[WordGenerator] JSON has no tiers!");
            UseFallbackWords();
            return;
        }

        foreach (var tier in data.tiers)
        {
            List<string> targetList = tier.difficulty switch
            {
                1 => easyWords,
                2 => mediumWords,
                3 => hardWords,
                _ => null
            };

            if (targetList == null)
            {
                Debug.LogWarning($"[WordGenerator] Unknown difficulty {tier.difficulty} in tier '{tier.tierName}', skipping.");
                continue;
            }

            // Filter: lowercase, trim, remove blocked, remove empty
            var filtered = tier.words
                .Select(w => w.ToLower().Trim())
                .Where(w => !string.IsNullOrEmpty(w))
                .Where(w => !blockedWords.Contains(w))
                .Distinct()
                .ToList();

            targetList.AddRange(filtered);
        }

        // Validate we actually have words
        if (easyWords.Count == 0 && mediumWords.Count == 0 && hardWords.Count == 0)
        {
            Debug.LogWarning("[WordGenerator] All tiers empty after filtering! Using fallbacks.");
            UseFallbackWords();
        }
    }

    private void UseFallbackWords()
    {
        easyWords = new List<string>(fallbackEasy);
        mediumWords = new List<string>(fallbackMedium);
        hardWords = new List<string>(fallbackHard);
        CurrentBankName = "fallback";
        IsLoaded = true;
    }

    #endregion
}