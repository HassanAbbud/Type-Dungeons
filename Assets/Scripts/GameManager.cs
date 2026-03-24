using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central game state manager for Type Dungeons.
/// Singleton — all other scripts communicate through events.
/// Owns: health, level, time, score.
/// Words route through WordDifficultyScaler → WordGenerator.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

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

    #region Events — UI, Sound, etc. subscribe to these
    public event Action<int, int> OnHealthChanged;        // (current, max)
    public event Action OnPlayerDied;
    public event Action<int> OnLevelChanged;              // newLevel
    public event Action<int> OnScoreChanged;              // newScore
    public event Action<float> OnTimerTick;               // remainingTime
    public event Action OnTimerExpired;
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnCoinsChanged;              // newCoinTotal
    public event Action<int, int> OnWordProgressChanged;  // (wordsThisLevel, wordsNeeded)
    #endregion

    public enum GameState { MainMenu, Playing, Paused, GameOver }

    #region Inspector Settings
    [Header("Player Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int startingLevel = 1;

    [Header("Timer Settings")]
    [SerializeField] private float levelTimeSeconds = 60f;
    [SerializeField] private float timePerLevelReduction = 5f;
    [SerializeField] private float minimumLevelTime = 20f;

    [Header("Level Progression")]
    [Tooltip("Words the player must complete to advance to the next level")]
    [SerializeField] private int wordsPerLevel = 10;
    [SerializeField] private int wordsPerLevelIncrease = 2;

    [Header("Scoring")]
    [SerializeField] private int baseScorePerWord = 100;
    [SerializeField] private float accuracyBonusMultiplier = 1.5f;

    #endregion

    #region Runtime State (read-only for other scripts)
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public int CurrentLevel { get; private set; }
    public int Score { get; private set; }
    public float RemainingTime { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int WordsCompletedThisLevel { get; private set; }
    public int WordsNeededThisLevel => wordsPerLevel + (CurrentLevel - 1) * wordsPerLevelIncrease;
    public int TotalWordsCompleted { get; private set; }

    // Accuracy — fed by the typing input script
    public int TotalKeysPressed { get; private set; }
    public int CorrectKeysPressed { get; private set; }
    public float Accuracy => TotalKeysPressed == 0 ? 1f : (float)CorrectKeysPressed / TotalKeysPressed;

    // Coins for store power-ups (Release 2.0 ready), nmendo16
    public int Coins { get; private set; }

    // Accuracy Bonus (formerly Shield Bonus) — Applies +20% accuracy multiplier to word scoring
    private float accuracyBonusMultiplierActive = 1.0f;
    public float AccuracyBonusMultiplierActive => accuracyBonusMultiplierActive;

    #endregion

    private Coroutine timerCoroutine;

    #region Public API

    /// <summary>Call from the Play button. Resets everything and starts the run.</summary>
    public void StartGame()
    {
        CurrentHealth = maxHealth;
        CurrentLevel = startingLevel;
        Score = 0;
        Coins = 1000;
        TotalKeysPressed = 0;
        CorrectKeysPressed = 0;
        WordsCompletedThisLevel = 0;
        TotalWordsCompleted = 0;
        RemainingTime = GetLevelTime();

        Time.timeScale = 1f;
        SetState(GameState.Playing);

        // Fire initial state so UI can update immediately
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnLevelChanged?.Invoke(CurrentLevel);
        OnScoreChanged?.Invoke(Score);
        OnCoinsChanged?.Invoke(Coins);
        OnWordProgressChanged?.Invoke(0, WordsNeededThisLevel);

        // Stop any leftover timer from a previous run, then start fresh
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(LevelTimerCoroutine());
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void TakeDamage(int amount = 1)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            SetState(GameState.GameOver);
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(int amount = 1)
    {
        if (CurrentState != GameState.Playing) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Called when the player successfully types a complete word.
    /// Handles scoring, coin drops, and level advancement.
    /// </summary>
    public void CompleteWord(float wordAccuracy, int wordLength)
    {
        if (CurrentState != GameState.Playing) return;

        // --- Scoring ---
        int wordScore = Mathf.RoundToInt(baseScorePerWord * wordLength * 0.5f);
        if (wordAccuracy >= 0.95f)
            wordScore = Mathf.RoundToInt(wordScore * accuracyBonusMultiplier);
        wordScore = Mathf.RoundToInt(wordScore * (1f + (CurrentLevel - 1) * 0.15f));

        // Apply active accuracy bonus (from Potion)
        wordScore = Mathf.RoundToInt(wordScore * accuracyBonusMultiplierActive);

        Score += wordScore;
        OnScoreChanged?.Invoke(Score);

        // --- Coins for store (Release 2.0) ---
        int coinsEarned = Mathf.Max(1, wordLength / 2) * 10;
        Coins += coinsEarned;
        OnCoinsChanged?.Invoke(Coins);

        // --- Level progression ---
        WordsCompletedThisLevel++;
        TotalWordsCompleted++;
        OnWordProgressChanged?.Invoke(WordsCompletedThisLevel, WordsNeededThisLevel);

        if (WordsCompletedThisLevel >= WordsNeededThisLevel)
            AdvanceLevel();
    }

    /// <summary>Feed keystroke data from the input system for accuracy tracking.</summary>
    public void RegisterKeystroke(bool correct)
    {
        TotalKeysPressed++;
        if (correct) CorrectKeysPressed++;
    }

    public string RequestNextWord()
    {
        if (WordDifficultyScaler.Instance != null)
            return WordDifficultyScaler.Instance.GetWord();

        Debug.LogWarning("[GameManager] WordDifficultyScaler not found! Check scene setup.");
        return "error";
    }

    // --- Power-up / Store helpers ---
    public void AddTime(float seconds) => RemainingTime += seconds;

    public void AddCoins(int amount)
    {
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void UpgradeMaxHealth(int amount = 1)
    {
        maxHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>Apply Accuracy Bonus (Potion): +20% accuracy multiplier to next words.</summary>
    public void ApplyAccuracyBonus()
    {
        accuracyBonusMultiplierActive = 1.2f;
        Debug.Log("[GameManager] Accuracy Bonus (Potion) activated: +20% multiplier");
    }

    /// <summary>Reset Accuracy Bonus (called on level up or game over).</summary>
    public void ResetAccuracyBonus()
    {
        accuracyBonusMultiplierActive = 1.0f;
        Debug.Log("[GameManager] Accuracy Bonus reset.");
    }

    #endregion

    #region Internals

    private void AdvanceLevel()
    {
        CurrentLevel++;
        WordsCompletedThisLevel = 0;
        RemainingTime = GetLevelTime();
        ResetAccuracyBonus();  // Clear bonus on level up
        OnLevelChanged?.Invoke(CurrentLevel);
        OnWordProgressChanged?.Invoke(0, WordsNeededThisLevel);
    }

    private float GetLevelTime()
    {
        float time = levelTimeSeconds - (CurrentLevel - 1) * timePerLevelReduction;
        return Mathf.Max(time, minimumLevelTime);
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        if (newState == GameState.GameOver)
        {
            ResetAccuracyBonus();  // Clear bonus on game over
            Time.timeScale = 0f;
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }
    }

    private IEnumerator LevelTimerCoroutine()
    {
        while (true)
        {
            if (CurrentState == GameState.Playing)
            {
                RemainingTime -= Time.deltaTime;
                OnTimerTick?.Invoke(RemainingTime);

                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    OnTimerExpired?.Invoke();
                    TakeDamage(1);

                    if (CurrentState == GameState.Playing)
                        RemainingTime = GetLevelTime();
                }
            }
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion
}