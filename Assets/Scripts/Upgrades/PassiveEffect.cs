using UnityEngine;

/// <summary>
/// Static utility that applies a passive item's effect to PlayerStats.
/// Extracts stat-modification logic from PassiveUpgradeOption into a single place.
/// </summary>
public static class PassiveEffect
{
    /// <summary>
    /// Apply one level of the given passive's effect to the player stats.
    /// </summary>
    public static void Apply(PlayerStats stats, PassiveData passive)
    {
        if (stats == null || passive == null) return;

        float bonus = passive.effectPerLevel;

        switch (passive.affectedStat)
        {
            case PassiveData.StatType.MoveSpeed:
                stats.MoveSpeed += bonus;
                break;
            case PassiveData.StatType.PickupRange:
                stats.PickupRange += bonus;
                break;
            case PassiveData.StatType.Armor:
                stats.Armor += Mathf.RoundToInt(bonus);
                break;
            case PassiveData.StatType.Luck:
                stats.Luck += bonus;
                break;
            case PassiveData.StatType.Regen:
                stats.Regen += bonus;
                break;
            case PassiveData.StatType.DamageMultiplier:
                stats.DamageMultiplier += bonus;
                break;
            case PassiveData.StatType.CooldownMultiplier:
                stats.CooldownMultiplier -= bonus;
                break;
            case PassiveData.StatType.AreaMultiplier:
                stats.AreaMultiplier += bonus;
                break;
            case PassiveData.StatType.ProjectileBonus:
                stats.ProjectileBonus += Mathf.RoundToInt(bonus);
                break;
            case PassiveData.StatType.MaxHP:
                stats.MaxHP += bonus;
                stats.Heal(bonus);
                break;
        }
    }
}