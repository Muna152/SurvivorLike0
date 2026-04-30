using UnityEngine;
using System;

/// <summary>
/// Base class for upgrade options shown during level-up.
/// </summary>
public abstract class UpgradeOption
{
    public string Name;
    public Sprite Icon;
    public string Description;

    public abstract void Apply();
}

/// <summary>Upgrade an existing weapon's level.</summary>
public class WeaponUpgradeOption : UpgradeOption
{
    private readonly WeaponBase _weapon;

    public WeaponUpgradeOption(WeaponBase weapon)
    {
        _weapon = weapon;
        Name = $"{weapon.Data.weaponName} Lv.{weapon.CurrentLevel} → Lv.{weapon.CurrentLevel + 1}";
        Icon = weapon.Data.icon;
        Description = GetLevelDescription(weapon);
    }

    public override void Apply()
    {
        _weapon.Upgrade();
    }

    private string GetLevelDescription(WeaponBase w)
    {
        if (w.Data == null || w.Data.levelData == null || w.Data.levelData.Length == 0)
            return "Upgrade weapon";

        int curIdx = Mathf.Clamp(w.CurrentLevel - 1, 0, w.Data.levelData.Length - 1);
        int nextIdx = Mathf.Clamp(w.CurrentLevel, 0, w.Data.levelData.Length - 1);
        var cur = w.Data.levelData[curIdx];
        var next = w.Data.levelData[nextIdx];

        var parts = new System.Collections.Generic.List<string>();

        int dmgDelta = next.damage - cur.damage;
        if (dmgDelta != 0) parts.Add($"DMG {cur.damage}→{next.damage}");

        float cdDelta = next.cooldown - cur.cooldown;
        if (Mathf.Abs(cdDelta) > 0.001f) parts.Add($"CD {cur.cooldown:F1}s→{next.cooldown:F1}s");

        int projDelta = next.projectileCount - cur.projectileCount;
        if (projDelta > 0) parts.Add($"+{projDelta} Projectile");

        int pierceDelta = next.pierce - cur.pierce;
        if (pierceDelta > 0) parts.Add($"+{pierceDelta} Pierce");

        float areaDelta = next.area - cur.area;
        if (Mathf.Abs(areaDelta) > 0.01f) parts.Add($"Area {cur.area:F1}→{next.area:F1}");

        float durDelta = next.duration - cur.duration;
        if (Mathf.Abs(durDelta) > 0.01f) parts.Add($"Dur {cur.duration:F1}s→{next.duration:F1}s");

        if (parts.Count == 0) parts.Add("Stats improved");

        // Special effect for max level
        if (nextIdx == w.Data.levelData.Length - 1 && !string.IsNullOrEmpty(w.Data.description))
            parts.Add($"★ {w.Data.description}");

        return string.Join(" | ", parts);
    }
}

/// <summary>Equip a new weapon.</summary>
public class NewWeaponOption : UpgradeOption
{
    private readonly WeaponData _data;
    private readonly PlayerWeaponManager _manager;

    public NewWeaponOption(WeaponData data, PlayerWeaponManager manager)
    {
        _data = data;
        _manager = manager;
        Name = $"New: {data.weaponName}";
        Icon = data.icon;
        Description = BuildNewWeaponDesc(data);
    }

    private string BuildNewWeaponDesc(WeaponData data)
    {
        var parts = new System.Collections.Generic.List<string>();

        // Type label
        string typeLabel = data.weaponType switch
        {
            WeaponData.WeaponType.Projectile => "Projectile",
            WeaponData.WeaponType.Orbital => "Orbital",
            WeaponData.WeaponType.Area => "Area",
            WeaponData.WeaponType.Auxiliary => "Support",
            _ => data.weaponType.ToString()
        };
        parts.Add($"[{typeLabel}]");

        if (data.levelData != null && data.levelData.Length > 0)
        {
            var ld = data.levelData[0];
            parts.Add($"DMG {ld.damage}");
            parts.Add($"CD {ld.cooldown:F1}s");
            if (ld.projectileCount > 1) parts.Add($"×{ld.projectileCount}");
            if (ld.pierce > 0) parts.Add($"Pierce {ld.pierce}");
            if (ld.area > 0) parts.Add($"Area {ld.area:F1}");
        }

        string desc = string.Join(" | ", parts);
        if (!string.IsNullOrEmpty(data.description))
            desc += $"\n{data.description}";
        return desc;
    }

    public override void Apply()
    {
        _manager.EquipWeapon(_data);
    }
}

/// <summary>Upgrade a passive stat.</summary>
public class PassiveUpgradeOption : UpgradeOption
{
    private readonly PassiveData _data;
    private readonly PlayerStats _stats;
    private readonly int _currentCount;

    public PassiveUpgradeOption(PassiveData data, PlayerStats stats, int currentCount)
    {
        _data = data;
        _stats = stats;
        _currentCount = currentCount;
        Name = $"{data.passiveName} Lv.{currentCount + 1}";
        Icon = data.icon;
        Description = FormatDescription(data, currentCount);
    }

    public override void Apply()
    {
        _stats.ApplyPassive(_data);
    }

    private static string FormatDescription(PassiveData data, int currentLevel)
    {
        float bonus = data.effectPerLevel;
        string sign = data.affectedStat == PassiveData.StatType.CooldownMultiplier ? "-" : "+";
        string statLabel = data.affectedStat.ToString();

        // Show next-level value
        float nextValue = bonus * (currentLevel + 1);
        float currentValue = bonus * currentLevel;

        // For special stats, show absolute value
        switch (data.affectedStat)
        {
            case PassiveData.StatType.MoveSpeed:
                return $"Move Speed {sign}{bonus:F1} (→{3f + nextValue:F1})";
            case PassiveData.StatType.PickupRange:
                return $"Pickup Range {sign}{bonus:F1} (→{1f + nextValue:F1})";
            case PassiveData.StatType.Armor:
                return $"Armor {sign}{Mathf.RoundToInt(bonus)} (→{Mathf.RoundToInt(nextValue)})";
            case PassiveData.StatType.Luck:
                return $"Luck {sign}{bonus:F0} (→{nextValue:F0})";
            case PassiveData.StatType.Regen:
                return $"HP Regen {sign}{bonus:F1}/s (→{nextValue:F1}/s)";
            case PassiveData.StatType.DamageMultiplier:
                return $"Damage {sign}{bonus * 100:F0}% (→{1f + nextValue:P0})";
            case PassiveData.StatType.CooldownMultiplier:
                return $"Cooldown {sign}{bonus * 100:F0}% (→{1f - nextValue:P0})";
            case PassiveData.StatType.AreaMultiplier:
                return $"Area {sign}{bonus * 100:F0}% (→{1f + nextValue:P0})";
            case PassiveData.StatType.ProjectileBonus:
                return $"Projectiles {sign}{Mathf.RoundToInt(bonus)} (→+{Mathf.RoundToInt(nextValue)})";
            case PassiveData.StatType.MaxHP:
                return $"Max HP {sign}{bonus:F0} (→{100f + nextValue:F0})";
            default:
                return $"{statLabel} {sign}{bonus:F1}";
        }
    }
}

/// <summary>Evolve a weapon (requires max level + required passive).</summary>
public class WeaponEvolutionOption : UpgradeOption
{
    private readonly WeaponBase _baseWeapon;
    private readonly PlayerWeaponManager _manager;

    public WeaponEvolutionOption(WeaponBase weapon, PlayerWeaponManager manager)
    {
        _baseWeapon = weapon;
        _manager = manager;
        var evolvedData = weapon.Data.evolvedWeapon;
        Name = $"进化: {weapon.Data.weaponName} → {evolvedData.weaponName}";
        Icon = weapon.Data.icon;
        Description = $"{weapon.Data.weaponName} 达到最大等级，进化为 {evolvedData.weaponName}！\n{evolvedData.description}";
    }

    public override void Apply()
    {
        _manager.CheckAndEvolveWeapons();
    }
}