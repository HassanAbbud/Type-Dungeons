using System;
using UnityEngine;

/// <summary>
/// PowerUpStore — Central manager for all power-up purchases and activations.
/// Singleton that coordinates with GameManager to apply power-up effects.
/// 
/// UPDATED LOGIC:
///   Potion (100 coins) → Boosts accuracy by 20% for next words
///   Shield (150 coins) → Freezes timer for 10 seconds
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
        Potion,  // +20% accuracy bonus (100 coins) -- SWAPPED
        Book,    // Freeze timer 5s (100 coins)
        Shield,  // Freeze timer 10s (150 coins) -- SWAPPED
        Clover   // Add 1 life (200 coins)
    }
    #endregion

    #region Power-up Prices (Data)
    [Header("Power-up Prices")]
    [SerializeField] private int swordPrice = 50;

    // MODIFIED: Potion is now 100
    [SerializeField] private int potionPrice = 100;

    [SerializeField] private int bookPrice = 100;

    // MODIFIED: Shield is now 150
    [SerializeField] private int shieldPrice = 150;

    [SerializeField] private int cloverPrice = 200;

    [Header("Power-up Effects")]
    [SerializeField] private int swordWordLength = 3;
    [SerializeField] private float timerFreezeDuration5 = 5f;
    [SerializeField] private float timerFreezeDuration10 = 10f;
    [SerializeField] private int cloverHealthBoost = 1;
    #endregion

    #region Events
    public event Action<PowerUpType> OnPowerUpPurchased;
    public event Action<PowerUpType> OnPowerUpActivated;
    public event Action<PowerUpType> OnPowerUpPurchaseFailed;
    #endregion

    #region Public API

    public bool TryPurchasePowerUp(PowerUpType powerUp)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PowerUpStore] GameManager not found!");
            return false;
        }

        int price = GetPowerUpPrice(powerUp);

        if (GameManager.Instance.Coins < price)
        {
            Debug.LogWarning($"[PowerUpStore] Cannot purchase {powerUp}: {GameManager.Instance.Coins} < {price}");
            OnPowerUpPurchaseFailed?.Invoke(powerUp);
            return false;
        }

        bool coinsSpent = GameManager.Instance.SpendCoins(price);
        if (!coinsSpent)
        {
            Debug.LogWarning($"[PowerUpStore] Failed to spend coins for {powerUp}");
            OnPowerUpPurchaseFailed?.Invoke(powerUp);
            return false;
        }

        OnPowerUpPurchased?.Invoke(powerUp);
        ActivatePowerUp(powerUp);

        return true;
    }

    public int GetPowerUpPrice(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => swordPrice,
            // MODIFIED: Map Potion to potionPrice (100)
            PowerUpType.Potion => potionPrice,
            PowerUpType.Book => bookPrice,
            // MODIFIED: Map Shield to shieldPrice (150)
            PowerUpType.Shield => shieldPrice,
            PowerUpType.Clover => cloverPrice,
            _ => 0
        };
    }

    public string GetPowerUpName(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => "Sword",
            PowerUpType.Potion => "Potion",
            PowerUpType.Book => "Book",
            PowerUpType.Shield => "Shield",
            PowerUpType.Clover => "Clover",
            _ => "Unknown"
        };
    }

    public string GetPowerUpDescription(PowerUpType powerUp)
    {
        return powerUp switch
        {
            PowerUpType.Sword => "Complete the current word instantly",
            // MODIFIED: Potion description
            PowerUpType.Potion => "Boost accuracy by 20% on next words",
            PowerUpType.Book => "Freeze timer for 5 seconds",
            // MODIFIED: Shield description
            PowerUpType.Shield => "Freeze timer for 10 seconds",
            PowerUpType.Clover => "Add 1 life",
            _ => "Unknown"
        };
    }

    #endregion

    #region Activation Logic (Private)

    private void ActivatePowerUp(PowerUpType powerUp)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        switch (powerUp)
        {
            case PowerUpType.Sword:
                ActivateSword(gm);
                break;
            // MODIFIED: Order doesn't strictly matter here due to switch, but logic inside matters
            case PowerUpType.Potion:
                ActivatePotion(gm);
                break;
            case PowerUpType.Book:
                ActivateBook(gm);
                break;
            case PowerUpType.Shield:
                ActivateShield(gm);
                break;
            case PowerUpType.Clover:
                ActivateClover(gm);
                break;
        }

        OnPowerUpActivated?.Invoke(powerUp);
        Debug.Log($"[PowerUpStore] {GetPowerUpName(powerUp)} activated!");
    }

    private void ActivateSword(GameManager gm)
    {
        gm.CompleteWord(1.0f, swordWordLength);
    }

    /// <summary>
    /// POTION (MODIFIED): Now applies accuracy bonus.
    /// </summary>
    private void ActivatePotion(GameManager gm)
    {
        gm.ApplyAccuracyBonus();
    }

    /// <summary>
    /// BOOK: Freeze the timer for 5 seconds.
    /// </summary>
    private void ActivateBook(GameManager gm)
    {
        gm.AddTime(timerFreezeDuration5);
    }

    /// <summary>
    /// SHIELD (MODIFIED): Now freezes timer for 10 seconds.
    /// </summary>
    private void ActivateShield(GameManager gm)
    {
        gm.AddTime(timerFreezeDuration10);
    }

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