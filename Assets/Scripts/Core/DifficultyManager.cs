using UnityEngine;

/// <summary>
/// Time-driven difficulty scaling system.
/// Provides global multipliers that increase over time to ramp up challenge.
/// Consumed by EnemyBase (HP), EnemySpawner (spawn interval), and other systems.
/// </summary>
public class DifficultyManager : Singleton<DifficultyManager>
{
    [Header("Scaling Coefficients")]
    [SerializeField] private float _hpGrowthRate = 0.08f;          // +8% HP per minute
    [SerializeField] private float _spawnAccelRate = 0.12f;       // spawn interval divisor growth per minute
    [SerializeField] private float _damageGrowthRate = 0.03f;     // +3% enemy damage per minute
    [SerializeField] private float _speedGrowthRate = 0.015f;     // +1.5% enemy move speed per minute

    [Header("State (Read-only)")]
    [SerializeField] private int _lastWholeMinute;

    /// <summary>Current elapsed minutes in the run.</summary>
    public float CurrentMinutes => GameManager.HasInstance
        ? GameManager.Instance.ElapsedTime / 60f
        : 0f;

    /// <summary>HP multiplier: baseHP × (1 + hpGrowthRate × minutes).</summary>
    public float HPMultiplier => 1f + _hpGrowthRate * CurrentMinutes;

    /// <summary>Spawn interval multiplier: 1 / (1 + spawnAccelRate × minutes).</summary>
    public float SpawnIntervalMultiplier => 1f / (1f + _spawnAccelRate * CurrentMinutes);

    /// <summary>Enemy damage multiplier: 1 + damageGrowthRate × minutes.</summary>
    public float DamageMultiplier => 1f + _damageGrowthRate * CurrentMinutes;

    /// <summary>Enemy move speed multiplier: 1 + speedGrowthRate × minutes.</summary>
    public float SpeedMultiplier => 1f + _speedGrowthRate * CurrentMinutes;

    /// <summary>Number of elite waves that have occurred so far.</summary>
    public int EliteWaveCount { get; private set; }

    /// <summary>Increment elite wave counter (called by EnemySpawner).</summary>
    public void OnEliteWaveSpawned() => EliteWaveCount++;

    /// <summary>Reset all difficulty state (called when a new run starts).</summary>
    public void ResetDifficulty()
    {
        EliteWaveCount = 0;
        _lastWholeMinute = 0;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        int currentMinute = Mathf.FloorToInt(CurrentMinutes);
        if (currentMinute > _lastWholeMinute)
        {
            _lastWholeMinute = currentMinute;
            GameEvents.InvokeDifficultyChanged(currentMinute);
        }
    }
}
