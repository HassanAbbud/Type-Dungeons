using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls a single enemy instance in the game scene.
/// Reads its configuration from an EnemyType ScriptableObject.
///
/// SETUP:
///   1. Create an Enemy prefab with: SpriteRenderer, this script, and a
///      child Canvas with a TMP_Text for the word bubble
///   2. EnemySpawner instantiates this prefab and calls Initialize(EnemyType)
///
/// INTEGRATES WITH:
///   - GameManager: coin drops, damage, score
///   - WordDifficultyScaler: word selection based on tier
///   - TypingInput: listens to OnWordDone to know when its word is typed
/// </summary>
public class EnemyController : MonoBehaviour
{
    #region Inspector (set on the prefab)
    [Header("Visual References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text wordBubbleText;
    [SerializeField] private Image healthBarFill;

    [Header("Optional")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    #endregion

    #region Events
    /// <summary>Fired when this enemy is defeated. EnemySpawner listens to spawn next.</summary>
    public event Action<EnemyController> OnDefeated;

    /// <summary>Fired when this enemy's timer expires and it attacks the player.</summary>
    public event Action<EnemyController> OnAttacked;
    #endregion

    #region Runtime State
    public EnemyType EnemyData { get; private set; }
    public string CurrentWord { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsCurrentTarget { get; private set; }

    private int wordsRemaining;
    private float timer;
    private bool hasCustomTimer;
    #endregion

    #region Public API

    /// <summary>
    /// Called by EnemySpawner after instantiation.
    /// Configures the enemy from its ScriptableObject data.
    /// </summary>
    public void Initialize(EnemyType type)
    {
        EnemyData = type;
        IsActive = true;
        wordsRemaining = type.wordsToDefeat;
        hasCustomTimer = type.customTimer > 0f;
        timer = type.customTimer;

        // Visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = type.sprite;
            spriteRenderer.color = type.tintColor;
            transform.localScale = Vector3.one * type.spriteScale;
        }

        // Animator
        if (animator != null && type.animatorController != null)
            animator.runtimeAnimatorController = type.animatorController;

        // Health bar
        UpdateHealthBar();

        // Get the first word
        AssignNewWord();

        // Spawn sound
        PlayClip(type.spawnSound);

        Debug.Log($"[Enemy] Spawned: {type.enemyName} | Words to defeat: {wordsRemaining} | Word: \"{CurrentWord}\"");
    }

    /// <summary>
    /// Mark this enemy as the active typing target.
    /// Only the current target's word should be shown to the player.
    /// </summary>
    public void SetAsTarget(bool isTarget)
    {
        IsCurrentTarget = isTarget;

        // Visual feedback — highlight when targeted
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isTarget
                ? Color.white
                : (EnemyData != null ? EnemyData.tintColor * 0.7f : Color.gray);
        }
    }

    /// <summary>
    /// Called when the player completes typing this enemy's current word.
    /// </summary>
    public void OnWordCompleted()
    {
        if (!IsActive) return;

        wordsRemaining--;
        UpdateHealthBar();

        if (wordsRemaining <= 0)
        {
            Defeat();
        }
        else
        {
            // Multi-word enemy: assign next word
            AssignNewWord();
        }
    }

    /// <summary>
    /// Force-kill this enemy (e.g. from a power-up).
    /// </summary>
    public void ForceDefeat()
    {
        if (!IsActive) return;
        wordsRemaining = 0;
        Defeat();
    }

    /// <summary>
    /// Get this enemy's word for display. Returns empty if not active.
    /// </summary>
    public string GetDisplayWord()
    {
        return IsActive ? CurrentWord : "";
    }

    #endregion

    #region Lifecycle

    private void Update()
    {
        if (!IsActive) return;

        // Custom per-enemy timer (optional)
        if (hasCustomTimer)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Attack();
            }
        }

        // Approach movement (visual only)
        if (EnemyData != null && EnemyData.approachSpeed > 0f)
        {
            transform.Translate(Vector3.left * EnemyData.approachSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Internal

    private void AssignNewWord()
    {
        if (EnemyData == null) return;

        // Get word based on tier override
        switch (EnemyData.wordTier)
        {
            case EnemyType.WordTierOverride.Easy:
                CurrentWord = WordDifficultyScaler.Instance?.GetWordFromTierName("Easy")
                              ?? GameManager.Instance?.RequestNextWord() ?? "type";
                break;

            case EnemyType.WordTierOverride.Medium:
                CurrentWord = WordDifficultyScaler.Instance?.GetWordFromTierName("Medium")
                              ?? GameManager.Instance?.RequestNextWord() ?? "sword";
                break;

            case EnemyType.WordTierOverride.Hard:
                CurrentWord = WordDifficultyScaler.Instance?.GetWordFromTierName("Hard")
                              ?? GameManager.Instance?.RequestNextWord() ?? "fortress";
                break;

            case EnemyType.WordTierOverride.MatchLevel:
            default:
                CurrentWord = GameManager.Instance?.RequestNextWord() ?? "type";
                break;
        }

        // Update word bubble
        if (wordBubbleText != null)
            wordBubbleText.text = CurrentWord;
    }

    private void Defeat()
    {
        IsActive = false;

        // Rewards
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(EnemyData.coinDrop);
            if (EnemyData.bonusScore > 0)
            {
                // Bonus score is added on top of the word completion score
                // (which GameManager already handled via CompleteWord)
            }
        }

        // Death visual
        if (spriteRenderer != null && EnemyData.deathSprite != null)
            spriteRenderer.sprite = EnemyData.deathSprite;

        // Death sound
        PlayClip(EnemyData.deathSound);

        // Notify spawner
        OnDefeated?.Invoke(this);

        // Destroy after a brief delay (for death animation)
        StartCoroutine(DestroyAfterDelay(0.5f));

        Debug.Log($"[Enemy] Defeated: {EnemyData.enemyName} | Dropped {EnemyData.coinDrop} coins");
    }

    private void Attack()
    {
        if (!IsActive) return;
        IsActive = false;

        // Deal damage to player
        GameManager.Instance?.TakeDamage(EnemyData.damage);

        // Attack sound
        PlayClip(EnemyData.attackSound);

        // Notify
        OnAttacked?.Invoke(this);

        // Remove
        StartCoroutine(DestroyAfterDelay(0.3f));

        Debug.Log($"[Enemy] {EnemyData.enemyName} attacked! Dealt {EnemyData.damage} damage.");
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || EnemyData == null) return;

        float ratio = (float)wordsRemaining / EnemyData.wordsToDefeat;
        healthBarFill.fillAmount = ratio;

        // Color: green → yellow → red
        healthBarFill.color = ratio > 0.5f
            ? Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f)
            : Color.Lerp(Color.red, Color.yellow, ratio * 2f);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    #endregion
}
