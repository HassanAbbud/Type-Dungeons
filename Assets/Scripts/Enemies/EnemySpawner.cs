using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Spawns enemies based on the current level.
/// Picks from available EnemyType ScriptableObjects, weighted by spawnWeight.
/// Manages the active enemy queue and feeds the current target to TypingInput.
///
/// SETUP:
///   1. Create Empty GO → name "_EnemySpawner"
///   2. Add this script
///   3. Drag your Enemy prefab into "Enemy Prefab" slot
///   4. Drag all EnemyType ScriptableObjects into "Enemy Types" array
///   5. Set spawn point transform (where enemies appear)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab with EnemyController + SpriteRenderer + word bubble")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("All available enemy types. Spawner picks from these based on level.")]
    [SerializeField] private EnemyType[] enemyTypes;

    [Tooltip("Where enemies spawn in world space")]
    [SerializeField] private Transform spawnPoint;

    [Header("Settings")]
    [Tooltip("Max enemies alive at once")]
    [SerializeField] private int maxActiveEnemies = 1;

    [Tooltip("Delay between spawns")]
    [SerializeField] private float spawnDelay = 0.5f;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private EnemyController currentTarget;
    private float spawnCooldown = 0f;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameState;
            GameManager.Instance.OnLevelChanged += HandleLevelChanged;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Spawn if we have room
        if (activeEnemies.Count < maxActiveEnemies)
        {
            spawnCooldown -= Time.deltaTime;
            if (spawnCooldown <= 0f)
            {
                SpawnEnemy();
                spawnCooldown = spawnDelay;
            }
        }

        // Ensure we always have a target
        if (currentTarget == null || !currentTarget.IsActive)
            AssignNextTarget();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameState;
            GameManager.Instance.OnLevelChanged -= HandleLevelChanged;
        }
    }

    #region Spawning

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || enemyTypes == null || enemyTypes.Length == 0) return;

        int level = GameManager.Instance?.CurrentLevel ?? 1;

        // Filter to types valid for this level
        var validTypes = enemyTypes.Where(t => t.CanSpawnAtLevel(level)).ToList();
        if (validTypes.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No valid enemy types for level " + level);
            return;
        }

        // Weighted random pick
        EnemyType chosen = WeightedRandom(validTypes);

        // Instantiate
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);

        EnemyController enemy = go.GetComponent<EnemyController>();
        if (enemy == null)
        {
            Debug.LogError("[EnemySpawner] Enemy prefab missing EnemyController!");
            Destroy(go);
            return;
        }

        enemy.Initialize(chosen);
        enemy.OnDefeated += HandleEnemyDefeated;
        enemy.OnAttacked += HandleEnemyAttacked;
        activeEnemies.Add(enemy);
    }

    private EnemyType WeightedRandom(List<EnemyType> types)
    {
        int totalWeight = 0;
        foreach (var t in types) totalWeight += t.spawnWeight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var t in types)
        {
            cumulative += t.spawnWeight;
            if (roll < cumulative) return t;
        }
        return types[0];
    }

    #endregion

    #region Target Management

    private void AssignNextTarget()
    {
        // Clean dead references
        activeEnemies.RemoveAll(e => e == null || !e.IsActive);

        if (activeEnemies.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // Pick first active enemy as target
        currentTarget = activeEnemies[0];
        currentTarget.SetAsTarget(true);

        // Dim all others
        for (int i = 1; i < activeEnemies.Count; i++)
            activeEnemies[i].SetAsTarget(false);
    }

    /// <summary>
    /// Called by TypingInput when the current word is completed.
    /// Tells the current target enemy that its word was typed.
    /// </summary>
    public void NotifyWordCompleted()
    {
        if (currentTarget != null && currentTarget.IsActive)
            currentTarget.OnWordCompleted();
    }

    /// <summary>Get the current target's word for TypingInput to display.</summary>
    public string GetCurrentTargetWord()
    {
        if (currentTarget != null && currentTarget.IsActive)
            return currentTarget.CurrentWord;
        return null;
    }

    /// <summary>Get the current active target enemy.</summary>
    public EnemyController GetCurrentTarget() => currentTarget;

    #endregion

    #region Event Handlers

    private void HandleEnemyDefeated(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
        enemy.OnDefeated -= HandleEnemyDefeated;
        enemy.OnAttacked -= HandleEnemyAttacked;

        if (currentTarget == enemy)
            currentTarget = null;
    }

    private void HandleEnemyAttacked(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
        enemy.OnDefeated -= HandleEnemyDefeated;
        enemy.OnAttacked -= HandleEnemyAttacked;

        if (currentTarget == enemy)
            currentTarget = null;
    }

    private void HandleGameState(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver || state == GameManager.GameState.MainMenu)
        {
            // Clear all enemies
            foreach (var e in activeEnemies)
            {
                if (e != null) Destroy(e.gameObject);
            }
            activeEnemies.Clear();
            currentTarget = null;
        }
    }

    private void HandleLevelChanged(int newLevel)
    {
        // Could spawn a special enemy on level-up, or clear remaining enemies
        spawnCooldown = 0f; // spawn immediately on new level
    }

    #endregion
}
