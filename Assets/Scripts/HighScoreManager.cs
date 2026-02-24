using System;
using System.Collections.Generic;
using UnityEngine;


public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxEntries = 10;
    [SerializeField] private string defaultPlayerName = "Player";

    /// Fired whenever the leaderboard changes. LeaderboardPanel subscribes here.
    public event Action<List<ScoreEntry>> OnLeaderboardChanged;

    /// Fired when a run qualifies as a new high score. (entry, 1-based rank)
    public event Action<ScoreEntry, int> OnNewHighScore;

    [Serializable]
    public class ScoreEntry
    {
        public string playerName;
        public int score;
        public float accuracy;     // 0.0 - 1.0
        public int wordsTyped;
        public int mistakes;
        public string date;         // yyyy-MM-dd

        public string AccuracyPercent => $"{Mathf.RoundToInt(accuracy * 100)}%";
    }

    [Serializable]
    private class ScoreList { public List<ScoreEntry> entries = new List<ScoreEntry>(); }

    private const string PrefsKey = "TypeDungeons_Leaderboard_v1";
    private List<ScoreEntry> scores = new List<ScoreEntry>();


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadScores();
    }

    private void OnEnable()
    {
        // Re-hook after scene load since GameManager may be a new instance
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied += AutoSubmitOnDeath;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied -= AutoSubmitOnDeath;
    }


    public int TrySubmitScore(string playerName, int score, float accuracy, int wordsTyped, int mistakes = 0)
    {
        var entry = new ScoreEntry
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? defaultPlayerName : playerName,
            score = score,
            accuracy = accuracy,
            wordsTyped = wordsTyped,
            mistakes = mistakes,
            date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        scores.Add(entry);
        scores.Sort((a, b) => b.score.CompareTo(a.score));

        if (scores.Count > maxEntries)
            scores.RemoveRange(maxEntries, scores.Count - maxEntries);

        int rank = scores.Contains(entry) ? scores.IndexOf(entry) + 1 : -1;

        SaveScores();
        OnLeaderboardChanged?.Invoke(scores);
        if (rank >= 1) OnNewHighScore?.Invoke(entry, rank);

        return rank;
    }

    /// <summary>Returns a sorted copy of all saved entries (best first).</summary>
    public List<ScoreEntry> GetTopScores() => new List<ScoreEntry>(scores);

    /// <summary>True if this score would make the leaderboard.</summary>
    public bool IsHighScore(int score) =>
        scores.Count < maxEntries || (scores.Count > 0 && score > scores[scores.Count - 1].score);

    /// <summary>Wipes all saved scores.</summary>
    public void ClearAllScores()
    {
        scores.Clear();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
        OnLeaderboardChanged?.Invoke(scores);
    }


    private void AutoSubmitOnDeath()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        TrySubmitScore(
            defaultPlayerName,
            gm.Score,
            gm.Accuracy,
            gm.TotalWordsCompleted,
            gm.TotalKeysPressed - gm.CorrectKeysPressed
        );
    }


    private void SaveScores()
    {
        string json = JsonUtility.ToJson(new ScoreList { entries = scores });
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }

    private void LoadScores()
    {
        if (!PlayerPrefs.HasKey(PrefsKey)) return;
        try
        {
            var wrapper = JsonUtility.FromJson<ScoreList>(PlayerPrefs.GetString(PrefsKey));
            if (wrapper?.entries != null)
            {
                scores = wrapper.entries;
                scores.Sort((a, b) => b.score.CompareTo(a.score));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HighScoreManager] Failed to load scores: {e.Message}");
        }
        OnLeaderboardChanged?.Invoke(scores);
    }
}