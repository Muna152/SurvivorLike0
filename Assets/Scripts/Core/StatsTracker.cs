using UnityEngine;

/// <summary>
/// Static utility for tracking cumulative player statistics, per-slot isolated via PlayerPrefs.
/// Key convention: Save_{slotIndex}_Stat_{statName}
/// </summary>
public static class StatsTracker
{
    private const string KeyTotalKills = "TotalKills";
    private const string KeyTotalGames = "TotalGames";
    private const string KeyBestTime = "BestTime";
    private const string KeyTotalGoldEarned = "TotalGoldEarned";

    private static string StatKey(string statName) =>
        $"Save_{SaveSlotManager.ActiveSlotIndex}_Stat_{statName}";

    private static bool HasSlot => SaveSlotManager.HasActiveSlot;

    // ── Total Kills ─────────────────────────────────────────────

    public static int GetTotalKills()
    {
        if (!HasSlot) return 0;
        return PlayerPrefs.GetInt(StatKey(KeyTotalKills), 0);
    }

    public static void AddTotalKills(int kills)
    {
        if (!HasSlot || kills <= 0) return;
        PlayerPrefs.SetInt(StatKey(KeyTotalKills), GetTotalKills() + kills);
        PlayerPrefs.Save();
    }

    // ── Total Games ─────────────────────────────────────────────

    public static int GetTotalGames()
    {
        if (!HasSlot) return 0;
        return PlayerPrefs.GetInt(StatKey(KeyTotalGames), 0);
    }

    public static void IncrementTotalGames()
    {
        if (!HasSlot) return;
        PlayerPrefs.SetInt(StatKey(KeyTotalGames), GetTotalGames() + 1);
        PlayerPrefs.Save();
    }

    // ── Best Survival Time ─────────────────────────────────────

    public static float GetBestTime()
    {
        if (!HasSlot) return 0f;
        return PlayerPrefs.GetFloat(StatKey(KeyBestTime), 0f);
    }

    public static void UpdateBestTime(float time)
    {
        if (!HasSlot || time <= 0f) return;
        if (time > GetBestTime())
        {
            PlayerPrefs.SetFloat(StatKey(KeyBestTime), time);
            PlayerPrefs.Save();
        }
    }

    // ── Total Gold Earned ──────────────────────────────────────

    public static int GetTotalGoldEarned()
    {
        if (!HasSlot) return 0;
        return PlayerPrefs.GetInt(StatKey(KeyTotalGoldEarned), 0);
    }

    public static void AddTotalGoldEarned(int gold)
    {
        if (!HasSlot || gold <= 0) return;
        PlayerPrefs.SetInt(StatKey(KeyTotalGoldEarned), GetTotalGoldEarned() + gold);
        PlayerPrefs.Save();
    }

    // ── Slot Cleanup ────────────────────────────────────────────

    /// <summary>Clear all stats for a given slot (called on slot deletion).</summary>
    public static void ClearSlotStats(int slotIndex)
    {
        string prefix = $"Save_{slotIndex}_Stat_";
        PlayerPrefs.DeleteKey(prefix + KeyTotalKills);
        PlayerPrefs.DeleteKey(prefix + KeyTotalGames);
        PlayerPrefs.DeleteKey(prefix + KeyBestTime);
        PlayerPrefs.DeleteKey(prefix + KeyTotalGoldEarned);
    }
}
