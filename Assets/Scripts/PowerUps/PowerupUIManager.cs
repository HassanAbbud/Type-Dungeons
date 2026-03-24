using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PowerUpUIManager — Manages the 5 power-up purchase buttons in gameplay.
/// Handles:
///   - Button enable/disable based on coin balance
///   - Button click detection and power-up purchase requests
///   - Click animation (scale effect)
///   - Sound feedback on click
///   - Visual state updates (button color/interactability)
/// 
/// Subscribes to GameManager.OnCoinsChanged to update button states dynamically.
/// Calls PowerUpStore.TryPurchasePowerUp() when buttons are clicked.
/// </summary>
public class PowerUpUIManager : MonoBehaviour
{
    #region Singleton
    public static PowerUpUIManager Instance { get; private set; }

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

    #region Button References (Configure in Inspector)
    [Header("=== Power-up Buttons ===")]
    [SerializeField] private Button swordButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private Button bookButton;
    [SerializeField] private Button potionButton;
    [SerializeField] private Button cloverButton;
    #endregion

    #region Visual Settings
    [Header("Button Visual States")]
    [SerializeField] private Color enabledButtonColor = Color.white;
    [SerializeField] private Color disabledButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
    [SerializeField] private float clickAnimationScale = 0.9f;
    [SerializeField] private float clickAnimationDuration = 0.1f;
    #endregion

    #region Runtime State
    private bool isInitialized = false;
    #endregion

    #region Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // Wire up button click listeners
        if (swordButton != null)
            swordButton.onClick.AddListener(() => OnPowerUpButtonClicked(PowerUpStore.PowerUpType.Sword, swordButton));
        if (shieldButton != null)
            shieldButton.onClick.AddListener(() => OnPowerUpButtonClicked(PowerUpStore.PowerUpType.Shield, shieldButton));
        if (bookButton != null)
            bookButton.onClick.AddListener(() => OnPowerUpButtonClicked(PowerUpStore.PowerUpType.Book, bookButton));
        if (potionButton != null)
            potionButton.onClick.AddListener(() => OnPowerUpButtonClicked(PowerUpStore.PowerUpType.Potion, potionButton));
        if (cloverButton != null)
            cloverButton.onClick.AddListener(() => OnPowerUpButtonClicked(PowerUpStore.PowerUpType.Clover, cloverButton));

        // Subscribe to game state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged += UpdateButtonStates;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // Initial state update
            UpdateButtonStates(GameManager.Instance.Coins);
        }

        // Subscribe to power-up events for visual feedback
        if (PowerUpStore.Instance != null)
        {
            PowerUpStore.Instance.OnPowerUpActivated += HandlePowerUpActivated;
        }

        isInitialized = true;
        Debug.Log("[PowerUpUIManager] Initialized successfully.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= UpdateButtonStates;
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (PowerUpStore.Instance != null)
        {
            PowerUpStore.Instance.OnPowerUpActivated -= HandlePowerUpActivated;
        }

        if (Instance == this) Instance = null;
    }

    #endregion

    #region Button State Management

    /// <summary>
    /// Update button enable/disable states based on current coin count.
    /// Called whenever coins change or game state changes.
    /// </summary>
    private void UpdateButtonStates(int currentCoins)
    {
        if (!isInitialized) return;

        bool isPlaying = GameManager.Instance != null &&
                        GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        UpdateButtonState(swordButton, PowerUpStore.PowerUpType.Sword, currentCoins, isPlaying);
        UpdateButtonState(shieldButton, PowerUpStore.PowerUpType.Shield, currentCoins, isPlaying);
        UpdateButtonState(bookButton, PowerUpStore.PowerUpType.Book, currentCoins, isPlaying);
        UpdateButtonState(potionButton, PowerUpStore.PowerUpType.Potion, currentCoins, isPlaying);
        UpdateButtonState(cloverButton, PowerUpStore.PowerUpType.Clover, currentCoins, isPlaying);
    }

    /// <summary>
    /// Update a single button's state (enabled/disabled).
    /// </summary>
    private void UpdateButtonState(Button button, PowerUpStore.PowerUpType powerUp, int currentCoins, bool isPlaying)
    {
        if (button == null || PowerUpStore.Instance == null) return;

        int powerUpPrice = PowerUpStore.Instance.GetPowerUpPrice(powerUp);
        bool canAfford = currentCoins >= powerUpPrice;
        bool shouldBeEnabled = canAfford && isPlaying;

        button.interactable = shouldBeEnabled;

        // Update button color to reflect state
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = shouldBeEnabled ? enabledButtonColor : disabledButtonColor;
        }

        // Update text color if it exists
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.color = shouldBeEnabled ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        // Optionally disable color highlight on transition if disabled
        if (!shouldBeEnabled)
        {
            var colors = button.colors;
            colors.disabledColor = disabledButtonColor;
            button.colors = colors;
        }
    }

    #endregion

    #region Button Click Handling

    /// <summary>
    /// Called when any power-up button is clicked.
    /// Attempts purchase, plays animation and sound.
    /// </summary>
    private void OnPowerUpButtonClicked(PowerUpStore.PowerUpType powerUp, Button clickedButton)
    {
        if (PowerUpStore.Instance == null)
        {
            Debug.LogError("[PowerUpUIManager] PowerUpStore not found!");
            return;
        }

        // Play click animation
        StartCoroutine(PlayClickAnimation(clickedButton));

        // Attempt to purchase the power-up
        bool purchaseSuccess = PowerUpStore.Instance.TryPurchasePowerUp(powerUp);

        if (purchaseSuccess)
        {
            // Play success sound
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            Debug.Log($"[PowerUpUIManager] {PowerUpStore.Instance.GetPowerUpName(powerUp)} purchased!");
        }
        else
        {
            // Optional: Play "denied" sound (using same click sound for now)
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            Debug.LogWarning($"[PowerUpUIManager] Failed to purchase {powerUp}");
        }
    }

    #endregion

    #region Visual Feedback

    /// <summary>
    /// Play button click animation (scale up/down).
    /// </summary>
    private IEnumerator PlayClickAnimation(Button button)
    {
        if (button == null) yield break;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector3 originalScale = rectTransform.localScale;
        Vector3 pressedScale = originalScale * clickAnimationScale;
        float elapsedTime = 0f;

        // Scale down
        while (elapsedTime < clickAnimationDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (clickAnimationDuration / 2f);
            rectTransform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            yield return null;
        }

        elapsedTime = 0f;

        // Scale back up
        while (elapsedTime < clickAnimationDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (clickAnimationDuration / 2f);
            rectTransform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
            yield return null;
        }

        // Ensure we're back to original scale
        rectTransform.localScale = originalScale;
    }

    /// <summary>
    /// Called when a power-up is activated (from PowerUpStore event).
    /// Could add visual effects here if needed.
    /// </summary>
    private void HandlePowerUpActivated(PowerUpStore.PowerUpType powerUp)
    {
        // Optional: Add UI flash or animation here
        Debug.Log($"[PowerUpUIManager] Visual feedback for {PowerUpStore.Instance.GetPowerUpName(powerUp)}");
    }

    #endregion

    #region Game State Handling

    /// <summary>
    /// Called when game state changes (Playing, Paused, GameOver, etc.)
    /// Disable buttons when not in Playing state.
    /// </summary>
    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (!isInitialized) return;

        if (GameManager.Instance != null)
        {
            UpdateButtonStates(GameManager.Instance.Coins);
        }
    }

    #endregion
}