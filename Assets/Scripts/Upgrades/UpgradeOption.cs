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
        if (w.Data == null || w.Data.levelData == null) return "Upgrade weapon";
        int nextIdx = Mathf.Clamp(w.CurrentLevel, 0, w.Data.levelData.Length - 1);
        var ld = w.Data.levelData[nextIdx];

        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"DMG {ld.damage}");
        parts.Add($"CD {ld.cooldown:F1}s");
        if (ld.projectileCount > 1) parts.Add($"×{ld.projectileCount}");
        if (ld.pierce > 0) parts.Add($"Pierce {ld.pierce}");
        if (ld.area > 0) parts.Add($"Area {ld.area:F1}");

        string baseDesc = w.Data.description ?? "";
        if (!string.IsNullOrEmpty(baseDesc) && baseDesc.Length > 30)
            baseDesc = baseDesc.Substring(0, 30) + "…";
        return string.Join(" | ", parts) + "\n" + baseDesc;
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
        string desc = data.description ?? "";
        if (data.levelData != null && data.levelData.Length > 0)
        {
            var ld = data.levelData[0];
            desc += $"\nDMG {ld.damage} | CD {ld.cooldown:F1}s";
            if (ld.projectileCount > 1) desc += $" | ×{ld.projectileCount}";
        }
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
        Description = $"+{data.effectPerLevel:F1} {data.affectedStat}";
    }

    public override void Apply()
    {
        float bonus = _data.effectPerLevel;
        switch (_data.affectedStat)
        {
            case PassiveData.StatType.MoveSpeed: _stats.MoveSpeed += bonus; break;
            case PassiveData.StatType.PickupRange: _stats.PickupRange += bonus; break;
            case PassiveData.StatType.Armor: _stats.Armor += Mathf.RoundToInt(bonus); break;
            case PassiveData.StatType.Luck: _stats.Luck += bonus; break;
            case PassiveData.StatType.Regen: _stats.Regen += bonus; break;
            case PassiveData.StatType.DamageMultiplier: _stats.DamageMultiplier += bonus; break;
            case PassiveData.StatType.CooldownMultiplier: _stats.CooldownMultiplier -= bonus; break;
            case PassiveData.StatType.AreaMultiplier: _stats.AreaMultiplier += bonus; break;
            case PassiveData.StatType.ProjectileBonus: _stats.ProjectileBonus += Mathf.RoundToInt(bonus); break;
            case PassiveData.StatType.MaxHP: _stats.MaxHP += bonus; break;
        }
    }
}