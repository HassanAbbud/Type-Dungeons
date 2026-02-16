using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Reference — drag Hassan's WordGenerator here")]
    [SerializeField] private WordGenerator wordGenerator;

    [Header("Difficulty Profiles")]
    [Tooltip("Define how word difficulty blends at each level range. Order matters — first matching profile wins.")]
    [SerializeField] private List<DifficultyProfile> profiles = new List<DifficultyProfile>()
    {
        // Defaults — tune in Inspector
        new DifficultyProfile { profileName = "Tutorial",    fromLevel = 1, toLevel = 2,  easyWeight = 1.0f, mediumWeight = 0.0f, hardWeight = 0.0f },
        new DifficultyProfile { profileName = "Early",       fromLevel = 3, toLevel = 4,  easyWeight = 0.4f, mediumWeight = 0.5f, hardWeight = 0.1f },
        new DifficultyProfile { profileName = "Mid",         fromLevel = 5, toLevel = 7,  easyWeight = 0.1f, mediumWeight = 0.5f, hardWeight = 0.4f },
        new DifficultyProfile { profileName = "Late",        fromLevel = 8, toLevel = 10, easyWeight = 0.0f, mediumWeight = 0.3f, hardWeight = 0.7f },
        new DifficultyProfile { profileName = "Endgame",     fromLevel = 11, toLevel = 99, easyWeight = 0.0f, mediumWeight = 0.1f, hardWeight = 0.9f },
    };

    [Header("Anti-Repeat")]
    [Tooltip("Track last N words to avoid repeats")]
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

    /// <summary>
    /// Maps to Hassan's 3 tiers. We pick a tier via weighted random,
    /// then pass a fake level to his GetRandomWord() to select from that tier.
    /// </summary>
    private enum WordTier { Easy, Medium, Hard }

    #endregion

    #region Runtime State
    private int currentLevel = 1;
    private Queue<string> recentWords = new Queue<string>();

    public event Action<string> OnDifficultyProfileChanged; // profile name
    #endregion

    #region Lifecycle

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= HandleLevelChanged;

        if (Instance == this) Instance = null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get a word with blended difficulty based on current level.
    /// Drop-in replacement for wordGenerator.GetRandomWord(level).
    /// </summary>
    public string GetWord()
    {
        if (wordGenerator == null)
        {
            Debug.LogError("[WordDifficultyScaler] WordGenerator reference not set!");
            return "error";
        }

        WordTier tier = RollTier();
        string word = GetWordFromTier(tier);

        // Anti-repeat: try a few times to avoid recently used words
        int attempts = 0;
        while (recentWords.Contains(word) && attempts < 10)
        {
            word = GetWordFromTier(tier);
            attempts++;
        }

        TrackWord(word);
        return word;
    }

    /// <summary>Get multiple unique words at once (for spawning a wave of enemies).</summary>
    public List<string> GetWords(int count)
    {
        var words = new List<string>(count);
        var usedThisBatch = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string word = GetWord();

            // Extra uniqueness within this batch
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

    /// <summary>Force a specific tier (e.g. boss = always hard, tutorial = always easy).</summary>
    public string GetWordFromTier(string tierName)
    {
        if (Enum.TryParse(tierName, true, out WordTier tier))
            return GetWordFromTier(tier);

        return GetWord(); // fallback to normal blended behavior
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
        recentWords.Clear(); // fresh pool each level

        // Notify if we crossed into a new profile
        var oldProfile = FindProfile(oldLevel);
        var newProfile = FindProfile(newLevel);
        if (oldProfile?.profileName != newProfile?.profileName)
            OnDifficultyProfileChanged?.Invoke(newProfile?.profileName ?? "Default");
    }

    private DifficultyProfile FindProfile(int level)
    {
        foreach (var profile in profiles)
        {
            if (level >= profile.fromLevel && level <= profile.toLevel)
                return profile;
        }
        // Past all profiles — return the last one (hardest)
        return profiles.Count > 0 ? profiles[profiles.Count - 1] : null;
    }

    /// <summary>Weighted random roll to pick which tier we pull from.</summary>
    private WordTier RollTier()
    {
        var profile = FindProfile(currentLevel);

        if (profile == null)
            return WordTier.Hard; // default to hard if no profiles configured

        float total = profile.easyWeight + profile.mediumWeight + profile.hardWeight;
        float roll = UnityEngine.Random.Range(0f, total);

        if (roll < profile.easyWeight) return WordTier.Easy;
        if (roll < profile.easyWeight + profile.mediumWeight) return WordTier.Medium;
        return WordTier.Hard;
    }

    private string GetWordFromTier(WordTier tier)
    {
        // These fake levels map to Hassan's tier thresholds.
        // If Hassan changes mediumLevelStart/hardLevelStart, update these.
        int fakeLevel = tier switch
        {
            WordTier.Easy   => 1,  // below mediumLevelStart (3)
            WordTier.Medium => 4,  // between mediumLevelStart and hardLevelStart
            WordTier.Hard   => 7,  // above hardLevelStart (6)
            _ => 1
        };

        return wordGenerator.GetRandomWord(fakeLevel);
    }

    private void TrackWord(string word)
    {
        recentWords.Enqueue(word);
        if (recentWords.Count > recentWordMemory)
            recentWords.Dequeue();
    }

    #endregion
}