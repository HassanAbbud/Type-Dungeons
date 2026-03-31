using UnityEngine;

/// <summary>
/// ScriptableObject that defines an enemy type.
/// Create in editor: Right-click → Create → TypeDungeons → Enemy Type
///
/// Each enemy type has a visual, a word difficulty tier, health, coin drops, etc.
/// EnemyController reads from this to configure itself at spawn time.
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Type", menuName = "TypeDungeons/Enemy Type")]
public class EnemyType : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Goblin";
    [TextArea] public string description = "A basic dungeon creature.";

    [Header("Visuals")]
    public Sprite sprite;
    public Sprite deathSprite;
    public Color tintColor = Color.white;
    public float spriteScale = 1f;

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;

    [Header("Combat")]
    [Tooltip("How many words the player must type to defeat this enemy")]
    [Range(1, 5)] public int wordsToDefeat = 1;

    [Tooltip("Which word tier to pull from: Easy, Medium, Hard, or MatchLevel (uses current difficulty profile)")]
    public WordTierOverride wordTier = WordTierOverride.MatchLevel;

    [Tooltip("Damage dealt to player if enemy's timer expires")]
    [Range(1, 3)] public int damage = 1;

    [Header("Timing")]
    [Tooltip("Seconds the player has to type this enemy's word(s). 0 = use level timer instead.")]
    public float customTimer = 0f;

    [Tooltip("Speed at which enemy approaches (for visual movement). 0 = stationary.")]
    public float approachSpeed = 0f;

    [Header("Rewards")]
    public int coinDrop = 1;
    public int bonusScore = 0;

    [Header("Spawn Rules")]
    [Tooltip("Minimum level where this enemy can appear")]
    public int minLevel = 1;
    [Tooltip("Maximum level where this enemy can appear. 99 = no cap.")]
    public int maxLevel = 99;
    [Tooltip("Relative spawn weight. Higher = more common.")]
    [Range(1, 10)] public int spawnWeight = 5;

    [Header("Audio")]
    public AudioClip spawnSound;
    public AudioClip deathSound;
    public AudioClip attackSound;

    /// <summary>Can this enemy spawn at the given level?</summary>
    public bool CanSpawnAtLevel(int level)
    {
        return level >= minLevel && level <= maxLevel;
    }

    public enum WordTierOverride
    {
        MatchLevel,  // uses WordDifficultyScaler's current profile
        Easy,
        Medium,
        Hard
    }
}
