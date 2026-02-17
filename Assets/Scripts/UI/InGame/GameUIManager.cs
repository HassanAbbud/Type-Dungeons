using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Manager for Type Dungeons — HUD focused.
/// The player types into a TMP_InputField. 
/// The target word is displayed above it.
/// Main menu code is commented out for later.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    /*
    #region Main Menu (UNCOMMENT LATER)
    [Header("=== MAIN MENU PANEL ===")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text titleText;
    #endregion
    */

    #region HUD
    [Header("=== HUD PANEL ===")]
    [SerializeField] private GameObject hudPanel;

    [Header("Health")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image[] heartIcons;

    [Header("Score & Coins")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image timerFillBar;

    [Header("Level & Progress")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text wordProgressText;
    [SerializeField] private TMP_Text difficultyProfileText;

    [Header("Word Display")]
    [Tooltip("Shows the target word the player needs to type")]
    [SerializeField] private TMP_Text targetWordText;

    [Header("Accuracy")]
    [SerializeField] private TMP_Text accuracyText;
    #endregion

    #region Pause
    [Header("=== PAUSE PANEL ===")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseQuitButton;
    #endregion

    #region Game Over
    [Header("=== GAME OVER PANEL ===")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalAccuracyText;
    [SerializeField] private TMP_Text finalLevelText;
    [SerializeField] private TMP_Text finalWordsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    #endregion

    [Header("Settings")]
    [SerializeField] private Color timerWarningColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private float timerWarningThreshold = 10f;

    private float maxLevelTime;

    #region Lifecycle

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnHealthChanged += UpdateHealth;
            gm.OnScoreChanged += UpdateScore;
            gm.OnCoinsChanged += UpdateCoins;
            gm.OnTimerTick += UpdateTimer;
            gm.OnLevelChanged += UpdateLevel;
            gm.OnWordProgressChanged += UpdateWordProgress;
            gm.OnGameStateChanged += HandleGameStateChanged;
            gm.OnPlayerDied += ShowGameOver;
        }

        if (WordDifficultyScaler.Instance != null)
            WordDifficultyScaler.Instance.OnDifficultyProfileChanged += UpdateDifficultyProfile;

        // Wire buttons
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (pauseQuitButton != null) pauseQuitButton.onClick.AddListener(OnPauseQuitClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);

        // HUD-only mode: start game immediately
        if (hudPanel != null) hudPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        GameManager.Instance?.StartGame();
    }

    private void Update()
    {
        // Pause toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.CurrentState == GameManager.GameState.Playing)
                gm.PauseGame();
            else if (gm.CurrentState == GameManager.GameState.Paused)
                gm.ResumeGame();
        }

        // Live accuracy
        if (accuracyText != null && GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            accuracyText.text = $"{GameManager.Instance.Accuracy * 100f:F1}%";
        }
    }

    private void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnHealthChanged -= UpdateHealth;
            gm.OnScoreChanged -= UpdateScore;
            gm.OnCoinsChanged -= UpdateCoins;
            gm.OnTimerTick -= UpdateTimer;
            gm.OnLevelChanged -= UpdateLevel;
            gm.OnWordProgressChanged -= UpdateWordProgress;
            gm.OnGameStateChanged -= HandleGameStateChanged;
            gm.OnPlayerDied -= ShowGameOver;
        }

        if (WordDifficultyScaler.Instance != null)
            WordDifficultyScaler.Instance.OnDifficultyProfileChanged -= UpdateDifficultyProfile;
    }

    #endregion

    #region Game State

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Playing:
                if (hudPanel != null) hudPanel.SetActive(true);
                if (pausePanel != null) pausePanel.SetActive(false);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                break;
            case GameManager.GameState.Paused:
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
        }
    }

    #endregion

    #region HUD Updates

    private void UpdateHealth(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"{current}/{max}";

        if (heartIcons != null)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] == null) continue;
                heartIcons[i].gameObject.SetActive(i < max);
                heartIcons[i].color = i < current
                    ? Color.white
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score:N0}";
    }

    private void UpdateCoins(int coins)
    {
        if (coinsText != null)
            coinsText.text = $"{coins}";
    }

    private void UpdateTimer(float remaining)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            timerText.text = $"{seconds}s";
            timerText.color = remaining <= timerWarningThreshold
                ? timerWarningColor : timerNormalColor;
        }

        if (timerFillBar != null && maxLevelTime > 0f)
            timerFillBar.fillAmount = Mathf.Clamp01(remaining / maxLevelTime);
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Level {level}";

        if (GameManager.Instance != null)
            maxLevelTime = GameManager.Instance.RemainingTime;
    }

    private void UpdateWordProgress(int completed, int needed)
    {
        if (wordProgressText != null)
            wordProgressText.text = $"{completed}/{needed}";
    }

    private void UpdateDifficultyProfile(string profileName)
    {
        if (difficultyProfileText != null)
            difficultyProfileText.text = profileName;
    }

    #endregion

    #region Game Over

    private void ShowGameOver()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (finalScoreText != null) finalScoreText.text = $"Score: {gm.Score:N0}";
        if (finalAccuracyText != null) finalAccuracyText.text = $"Accuracy: {gm.Accuracy * 100f:F1}%";
        if (finalLevelText != null) finalLevelText.text = $"Level Reached: {gm.CurrentLevel}";
        if (finalWordsText != null) finalWordsText.text = $"Words Typed: {gm.TotalWordsCompleted}";
    }

    #endregion

    #region Button Callbacks

    private void OnResumeClicked()
    {
        SoundManager.PlaySound(SoundType.BTN_CLICK);
        GameManager.Instance?.ResumeGame();
    }

    private void OnPauseQuitClicked()
    {
        SoundManager.PlaySound(SoundType.BTN_CLICK);
        Time.timeScale = 1f;
        GameManager.Instance?.StartGame();
    }

    private void OnRetryClicked()
    {
        SoundManager.PlaySound(SoundType.BTN_CLICK);
        GameManager.Instance?.StartGame();
    }

    private void OnMenuClicked()
    {
        SoundManager.PlaySound(SoundType.BTN_CLICK);
        Time.timeScale = 1f;
        GameManager.Instance?.StartGame();
    }

    #endregion

    #region Public — called by TypingInput

    /// <summary>Display the target word above the input field.</summary>
    public void DisplayWord(string word)
    {
        if (targetWordText != null)
            targetWordText.text = word;
    }

    /// <summary>
    /// Update the target word display to show progress.
    /// Green = typed portion, white = remaining.
    /// </summary>
    public void UpdateTypedProgress(string fullWord, int charIndex)
    {
        if (targetWordText == null) return;

        string typed = fullWord.Substring(0, charIndex);
        string remaining = fullWord.Substring(charIndex);
        targetWordText.text = $"<color=#4CAF50>{typed}</color>{remaining}";
    }

    /// <summary>Flash the target word red on a wrong keystroke.</summary>
    public void FlashWrongKey()
    {
        if (targetWordText != null)
            StartCoroutine(FlashColor(targetWordText, Color.red, 0.15f));
    }

    private IEnumerator FlashColor(TMP_Text text, Color flashColor, float duration)
    {
        Color original = text.color;
        text.color = flashColor;
        yield return new WaitForSecondsRealtime(duration);
        text.color = original;
    }

    #endregion
}