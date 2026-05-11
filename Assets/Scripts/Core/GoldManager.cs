using UnityEngine;

/// <summary>
/// Static utility for persistent gold and permanent upgrades, per-slot isolated via PlayerPrefs.
/// Follows the same key convention as SaveSlotManager: Save_{slotIndex}_Gold / Save_{slotIndex}_Upgrade_{type}.
/// </summary>
public static class GoldManager
{
    // ── Upgrade Type Enum ───────────────────────────────────────

    public enum PermanentUpgradeType
    {
        HPBonus,            // +5% MaxHP per level
        MoveSpeedBonus,     // +3% MoveSpeed per level
        DamageBonus,        // +5% DamageMultiplier per level
        PickupRangeBonus,   // +10% PickupRange per level
        ExtraLife           // +1 revive per level
    }

    // ── Upgrade Data (from GDD 9.2) ────────────────────────────

    private static readonly int[] MaxLevels =
    {
        5, // HPBonus
        5, // MoveSpeedBonus
        5, // DamageBonus
        5, // PickupRangeBonus
        3  // ExtraLife
    };

    private static readonly int[][] UpgradeCosts =
    {
        new[] { 50, 100, 200, 400, 800 },       // HPBonus
        new[] { 50, 100, 200, 400, 800 },       // MoveSpeedBonus
        new[] { 50, 100, 200, 400, 800 },       // DamageBonus
        new[] { 20, 40, 80, 160, 320 },         // PickupRangeBonus
        new[] { 300, 600, 1200 }                 // ExtraLife
    };

    private static readonly string[] UpgradeNames =
    {
        "HP 加成",
        "移速加成",
        "伤害加成",
        "拾取范围",
        "额外生命"
    };

    private static readonly string[] UpgradeDescriptions =
    {
        "+5% 生命值",
        "+3% 移动速度",
        "+5% 伤害",
        "+10% 拾取范围",
        "复活1次 (50% HP)"
    };

    // ── Key Helpers ─────────────────────────────────────────────

    private static string GoldKey => $"Save_{SaveSlotManager.ActiveSlotIndex}_Gold";
    private static string UpgradeKey(PermanentUpgradeType type) =>
        $"Save_{SaveSlotManager.ActiveSlotIndex}_Upgrade_{type}";

    private static bool HasSlot => SaveSlotManager.HasActiveSlot;

    // ── Gold CRUD ───────────────────────────────────────────────

    /// <summary>Get persistent gold balance for the active save slot.</summary>
    public static int GetGold()
    {
        if (!HasSlot) return 0;
        return PlayerPrefs.GetInt(GoldKey, 0);
    }

    /// <summary>Add gold to the persistent balance (called on game-end).</summary>
    public static void AddGold(int amount)
    {
        if (!HasSlot || amount <= 0) return;
        int current = GetGold();
        PlayerPrefs.SetInt(GoldKey, current + amount);
        PlayerPrefs.Save();
    }

    /// <summary>Spend gold from the persistent balance. Returns true if successful.</summary>
    public static bool SpendGold(int cost)
    {
        if (!HasSlot || cost <= 0) return false;
        int current = GetGold();
        if (current < cost) return false;
        PlayerPrefs.SetInt(GoldKey, current - cost);
        PlayerPrefs.Save();
        return true;
    }

    // ── Permanent Upgrades ──────────────────────────────────────

    /// <summary>Current upgrade level (0 = not purchased).</summary>
    public static int GetUpgradeLevel(PermanentUpgradeType type)
    {
        if (!HasSlot) return 0;
        return PlayerPrefs.GetInt(UpgradeKey(type), 0);
    }

    /// <summary>Set upgrade level directly (for debug or data migration).</summary>
    public static void SetUpgradeLevel(PermanentUpgradeType type, int level)
    {
        if (!HasSlot) return;
        PlayerPrefs.SetInt(UpgradeKey(type), Mathf.Clamp(level, 0, GetMaxLevel(type)));
        PlayerPrefs.Save();
    }

    /// <summary>Max level for the given upgrade type.</summary>
    public static int GetMaxLevel(PermanentUpgradeType type)
    {
        return MaxLevels[(int)type];
    }

    /// <summary>Cost to upgrade from current level to next level. Returns -1 if already max.</summary>
    public static int GetUpgradeCost(PermanentUpgradeType type)
    {
        int current = GetUpgradeLevel(type);
        int max = GetMaxLevel(type);
        if (current >= max) return -1;
        return UpgradeCosts[(int)type][current];
    }

    /// <summary>Display name for the upgrade type.</summary>
    public static string GetUpgradeName(PermanentUpgradeType type)
    {
        return UpgradeNames[(int)type];
    }

    /// <summary>Effect description per level.</summary>
    public static string GetUpgradeDescription(PermanentUpgradeType type)
    {
        return UpgradeDescriptions[(int)type];
    }

    /// <summary>
    /// Purchase one level of the upgrade. Checks gold balance and level cap.
    /// Returns true if purchase succeeded.
    /// </summary>
    public static bool PurchaseUpgrade(PermanentUpgradeType type)
    {
        int cost = GetUpgradeCost(type);
        if (cost < 0) return false;
        if (!SpendGold(cost)) return false;

        int newLevel = GetUpgradeLevel(type) + 1;
        SetUpgradeLevel(type, newLevel);
        GameEvents.InvokeOnPermanentUpgradePurchased();
        return true;
    }

    /// <summary>Apply all permanent upgrades to the given PlayerStats instance.</summary>
    public static void ApplyPermanentUpgrades(PlayerStats stats)
    {
        if (stats == null || !HasSlot) return;

        // HP Bonus: +5% per level (multiplicative)
        int hpLevel = GetUpgradeLevel(PermanentUpgradeType.HPBonus);
        for (int i = 0; i < hpLevel; i++)
            stats.MaxHP *= 1.05f;

        // Move Speed Bonus: +3% per level (additive multiplier)
        int speedLevel = GetUpgradeLevel(PermanentUpgradeType.MoveSpeedBonus);
        stats.MoveSpeed *= (1f + 0.03f * speedLevel);

        // Damage Bonus: +5% per level (additive to multiplier)
        int dmgLevel = GetUpgradeLevel(PermanentUpgradeType.DamageBonus);
        stats.DamageMultiplier += 0.05f * dmgLevel;

        // Pickup Range Bonus: +10% of base (2.5) per level = +0.25 per level
        int pickupLevel = GetUpgradeLevel(PermanentUpgradeType.PickupRangeBonus);
        stats.PickupRange += 0.25f * pickupLevel;

        // Extra Life: +1 revive per level
        int extraLifeLevel = GetUpgradeLevel(PermanentUpgradeType.ExtraLife);
        stats.ExtraLives = extraLifeLevel;

        // Clamp HP after MaxHP changes
        stats.ClampCurrentHP();
    }

    // ── Slot Cleanup ────────────────────────────────────────────

    /// <summary>Clear all gold and upgrade data for a given slot (called on slot deletion).</summary>
    public static void ClearSlotData(int slotIndex)
    {
        PlayerPrefs.DeleteKey($"Save_{slotIndex}_Gold");
        for (int i = 0; i < System.Enum.GetValues(typeof(PermanentUpgradeType)).Length; i++)
        {
            PlayerPrefs.DeleteKey($"Save_{slotIndex}_Upgrade_{(PermanentUpgradeType)i}");
        }
    }
}
