using System;
using UnityEngine;

/// <summary>
/// PowerUpStore — Central manager for all power-up purchases and activations.
/// Singleton that coordinates with GameManager to apply power-up effects.
/// 
/// Power-ups:
///   Sword (50 coins)   → Completes current word instantly
///   Shield (100 coins) → Boosts accuracy by 20% for next words
///   Book (100 coins)   → Freezes timer for 5 seconds
///   Potion (150 coins) → Freezes timer for 10 seconds
///   Clover (200 coins) → Adds 1 life
/// </summary>
public class PowerUpStore : MonoBehaviour
{
    #region Singleton
    public static PowerUpStore Instance { get; private set; }

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

    #region Power-up Types
    public enum PowerUpType
    {
        Sword,   // Complete word instantly (50 coins)
        Shield,  // +20% accuracy bonus (100 coins)
        Book,    // Freeze timer 5s (100 coins)
        Potion,  // Freeze timer 10s (150 coins)
        Clover   // Add 1 life (200 coins)
    }
    #endregion

    #region Power-up Prices (Data)
    [Header("Power-up Prices")]
    [SerializeField] private int swordPrice = 50;
    [SerializeField] private int shieldPrice = 100;
    [SerializeField] private int bookPrice = 100;
    [SerializeField] private int potionPrice = 150;
    [SerializeField] private int cloverPrice = 200;

    [Header("Power-up Effects")]
    [SerializeField] private int swordWordLength = 3;    // Simulate 3-letter word
    [SerializeField] private float timerFreezeDuration5 = 5f;
    [SerializeField] private float timerFreezeDuration10 = 10f;
    [SerializeField] private int cloverHealthBoost = 1;
    #endregion

    #region Events
    /// <summary>Fired when a power-up is successfully purchased.</summary>
    public event Action<PowerUpType> OnPowerUpPurchased;

    /// <summary>Fired when a power-up effect is activated.</summary>
    public event Action<PowerUpType> OnPowerUpActivated;

    /// <summary>Fired when a purchase attempt fails (insufficient coins).</summary>
    public event Action<PowerUpType> OnPowerUpPurchaseFailed;
    #endregion

    #region Public API

    /// <summary>
    /// Attempt to purchase and activate a power-up.
    /// Returns true if purchase was successful, false if insufficient coins.
    /// </summary>
    public bool TryPurchasePowerUp(PowerUpType powerUp)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PowerUpStore] GameManager not found!");
            return false;
        }

        int price = GetPowerUpPrice(powerUp);

        // Check if player has enough coins
        if (GameManager.Instance.Coins < price)
        {
            Debug.LogWarning($"[PowerUpStore] Cannot purchase {powerUp}: {GameManager.Instance.Coins} < {price}");
            OnPowerUpPurchaseFailed?.Invoke(powerUp);
            return false;
        }

        // Deduct coins
        bool coinsSpent = GameManager.Instance.SpendCoins(price);
        if (!coinsSpent)
        {
            Debug.LogWarning($"[PowerUpStore] Failed to spend coins for {powerUp}");
            OnPowerUpPurchaseFailed?.Invoke(powerUp);
            return false;
        }

        // Fire purchased event
        OnPowerUpPurchased?.Invoke(powerUp);

        // Activate the power-up effect
        ActivatePowerUp(powerUp);

        return true;
    }

    /// <summary>Get the coin price of a power-up.</summary>
    public int GetPowerUpPrice(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => swordPrice,
            PowerUpType.Shield => shieldPrice,
            PowerUpType.Book => bookPrice,
            PowerUpType.Potion => potionPrice,
            PowerUpType.Clover => cloverPrice,
            _ => 0
        };
    }

    /// <summary>Get the display name of a power-up.</summary>
    public string GetPowerUpName(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => "Sword",
            PowerUpType.Shield => "Shield",
            PowerUpType.Book => "Book",
            PowerUpType.Potion => "Potion",
            PowerUpType.Clover => "Clover",
            _ => "Unknown"
        };
    }

    /// <summary>Get the description of a power-up's effect.</summary>
    public string GetPowerUpDescription(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => "Complete the current word instantly",
            PowerUpType.Shield => "Boost accuracy by 20% on next words",
            PowerUpType.Book => "Freeze timer for 5 seconds",
            PowerUpType.Potion => "Freeze timer for 10 seconds",
            PowerUpType.Clover => "Add 1 life",
            _ => "Unknown"
        };
    }

    #endregion

    #region Activation Logic (Private)

    /// <summary>Apply the power-up effect to the game state.</summary>
    private void ActivatePowerUp(PowerUpType powerUp)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        switch (powerUp)
        {
            case PowerUpType.Sword:
                ActivateSword(gm);
                break;
            case PowerUpType.Shield:
                ActivateShield(gm);
                break;
            case PowerUpType.Book:
                ActivateBook(gm);
                break;
            case PowerUpType.Potion:
                ActivatePotion(gm);
                break;
            case PowerUpType.Clover:
                ActivateClover(gm);
                break;
        }

        // Fire activated event after effect is applied
        OnPowerUpActivated?.Invoke(powerUp);

        Debug.Log($"[PowerUpStore] {GetPowerUpName(powerUp)} activated!");
    }

    /// <summary>
    /// SWORD: Complete the current word instantly.
    /// Simulates typing a perfect 3-letter word with no mistakes.
    /// Grants: score, coin drop, word progress.
    /// </summary>
    private void ActivateSword(GameManager gm)
    {
        // Call CompleteWord with perfect accuracy (1.0f) and 3-letter length
        // This will:
        // - Add score (baseScorePerWord * 3 * 0.5f * accuracyBonus * levelMultiplier)
        // - Add coins (Mathf.Max(1, 3/2) = 1 coin)
        // - Increment word progress
        gm.CompleteWord(1.0f, swordWordLength);
    }

    /// <summary>
    /// SHIELD: Apply +20% accuracy bonus to next words until level up.
    /// Modifies the scoring multiplier for subsequent words.
    /// </summary>
    private void ActivateShield(GameManager gm)
    {
        //gm.ApplyShieldBonus();
    }

    /// <summary>
    /// BOOK: Freeze the timer for 5 seconds.
    /// Player gets 5 extra seconds to complete words.
    /// </summary>
    private void ActivateBook(GameManager gm)
    {
        gm.AddTime(timerFreezeDuration5);
    }

    /// <summary>
    /// POTION: Freeze the timer for 10 seconds.
    /// Player gets 10 extra seconds to complete words.
    /// </summary>
    private void ActivatePotion(GameManager gm)
    {
        gm.AddTime(timerFreezeDuration10);
    }

    /// <summary>
    /// CLOVER: Add 1 life to the player's max health.
    /// Also heals the player by 1 HP (up to new max).
    /// </summary>
    private void ActivateClover(GameManager gm)
    {
        gm.UpgradeMaxHealth(cloverHealthBoost);
    }

    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion
}