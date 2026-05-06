using UnityEngine;

/// <summary>
/// Structured unlock condition for characters.
/// Supports multiple unlock types with configurable thresholds.
/// </summary>
[System.Serializable]
public class UnlockCondition
{
    public enum UnlockType
    {
        SurviveTime,    // Survive for N seconds
        KillCount,      // Kill N enemies in a single run
        HealAmount,     // Heal N HP in a single run
        ReachLevel,     // Reach player level N
        KillElites      // Kill N elite enemies in a single run
    }

    [Tooltip("Type of condition to check")]
    public UnlockType type;

    [Tooltip("Threshold value to meet (e.g. 300 seconds for SurviveTime)")]
    public float threshold;

    /// <summary>Check if this condition is met given the current player stats and game time.</summary>
    public bool IsUnlocked(PlayerStats stats, float elapsedTime)
    {
        if (stats == null) return false;

        return type switch
        {
            UnlockType.SurviveTime => elapsedTime >= threshold,
            UnlockType.KillCount  => stats.KillCount >= threshold,
            UnlockType.HealAmount => stats.TotalHealed >= threshold,
            UnlockType.ReachLevel => stats.Level >= threshold,
            UnlockType.KillElites => stats.EliteKillCount >= threshold,
            _ => false
        };
    }

    /// <summary>Human-readable description of the unlock condition.</summary>
    public string Description => type switch
    {
        UnlockType.SurviveTime => $"存活 {Mathf.CeilToInt(threshold / 60f)} 分钟",
        UnlockType.KillCount  => $"击杀 {Mathf.CeilToInt(threshold)} 个敌人",
        UnlockType.HealAmount => $"治疗 {Mathf.CeilToInt(threshold)} 点生命",
        UnlockType.ReachLevel => $"达到 {Mathf.CeilToInt(threshold)} 级",
        UnlockType.KillElites => $"击杀 {Mathf.CeilToInt(threshold)} 个精英敌人",
        _ => "未知条件"
    };
}
