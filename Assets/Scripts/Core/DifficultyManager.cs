using UnityEngine;

/// <summary>
/// Time-driven difficulty scaling system.
/// Provides global multipliers that increase over time to ramp up challenge.
/// Consumed by EnemyBase (HP), EnemySpawner (spawn interval), and other systems.
/// </summary>
public class DifficultyManager : Singleton<DifficultyManager>
{
    private int _lastWholeMinute;

    /// <summary>Current elapsed minutes in the run.</summary>
    public float CurrentMinutes => GameManager.HasInstance
        ? GameManager.Instance.ElapsedTime / 60f
        : 0f;

    /// <summary>HP multiplier: baseHP × (1 + hpGrowthRate × minutes).</summary>
    public float HPMultiplier
    {
        get
        {
            float rate = GameBalanceConfig.Instance != null
                ? GameBalanceConfig.Instance.hpGrowthRate : 0.08f;
            return 1f + rate * CurrentMinutes;
        }
    }

    /// <summary>Spawn interval multiplier: 1 / (1 + spawnAccelRate × minutes).</summary>
    public float SpawnIntervalMultiplier
    {
        get
        {
            float rate = GameBalanceConfig.Instance != null
                ? GameBalanceConfig.Instance.spawnAccelRate : 0.12f;
            return 1f / (1f + rate * CurrentMinutes);
        }
    }

    /// <summary>Enemy damage multiplier: 1 + damageGrowthRate × minutes.</summary>
    public float DamageMultiplier
    {
        get
        {
            float rate = GameBalanceConfig.Instance != null
                ? GameBalanceConfig.Instance.damageGrowthRate : 0.03f;
            return 1f + rate * CurrentMinutes;
        }
    }

    /// <summary>Enemy move speed multiplier: 1 + speedGrowthRate × minutes.</summary>
    public float SpeedMultiplier
    {
        get
        {
            float rate = GameBalanceConfig.Instance != null
                ? GameBalanceConfig.Instance.speedGrowthRate : 0.015f;
            return 1f + rate * CurrentMinutes;
        }
    }

    /// <summary>Number of elite waves that have occurred so far.</summary>
    public int EliteWaveCount { get; private set; }

    /// <summary>Increment elite wave counter (called by EnemySpawner).</summary>
    public void OnEliteWaveSpawned()
    {
        EliteWaveCount++;
        _accumulatedDecayTimer = 0f;
    }

    /// <summary>Reset all difficulty state (called when a new run starts).</summary>
    public void ResetDifficulty()
    {
        EliteWaveCount = 0;
        _lastWholeMinute = 0;
        _accumulatedDecayTimer = 0f;
    }

    // Elite wave decay: reduce bonus over time between elite waves
    private float _accumulatedDecayTimer;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        int currentMinute = Mathf.FloorToInt(CurrentMinutes);
        if (currentMinute > _lastWholeMinute)
        {
            _lastWholeMinute = currentMinute;
            GameEvents.InvokeDifficultyChanged(currentMinute);
        }

        // Elite wave bonus decay: each minute without an elite wave, reduce the count
        var cfg = GameBalanceConfig.Instance;
        int decayRate = cfg != null ? cfg.eliteWaveDecayPerWave : 1;
        if (decayRate > 0 && EliteWaveCount > 0)
        {
            _accumulatedDecayTimer += Time.deltaTime;
            if (_accumulatedDecayTimer >= 60f) // Every 60 seconds
            {
                EliteWaveCount = Mathf.Max(0, EliteWaveCount - decayRate);
                _accumulatedDecayTimer = 0f;
            }
        }
    }
}
