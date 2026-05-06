using UnityEngine;

/// <summary>
/// Static utility class for managing up to 3 save slots via PlayerPrefs.
/// Each slot has a name (string key) and stores per-slot unlock data.
/// UnlockManager reads the active slot index to prefix its PlayerPrefs keys.
/// </summary>
public static class SaveSlotManager
{
    public const int MAX_SLOTS = 3;

    private const string ActiveSlotKey = "SaveSlot_Active";
    private const string SlotNameKeyPrefix = "SaveSlot_";
    private const string SlotNameKeySuffix = "_Name";
    private const string SlotUnlockKeyPrefix = "Save_";

    // ── Active Slot ──────────────────────────────────────────────

    /// <summary>Currently active save slot index (0-2), or -1 if none selected.</summary>
    public static int ActiveSlotIndex
    {
        get => PlayerPrefs.GetInt(ActiveSlotKey, -1);
        private set
        {
            PlayerPrefs.SetInt(ActiveSlotKey, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Is there an active save slot selected?</summary>
    public static bool HasActiveSlot => ActiveSlotIndex >= 0 && ActiveSlotIndex < MAX_SLOTS;

    // ── Slot Info ────────────────────────────────────────────────

    /// <summary>Get the name of a save slot. Returns empty string if slot is empty.</summary>
    public static string GetSlotName(int index)
    {
        if (!IsValidIndex(index)) return "";
        return PlayerPrefs.GetString(GetSlotNameKey(index), "");
    }

    /// <summary>Does the slot at this index have a name (i.e. exists)?</summary>
    public static bool HasSlot(int index)
    {
        if (!IsValidIndex(index)) return false;
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(GetSlotNameKey(index), ""));
    }

    /// <summary>Check if a name is already used by any existing slot.</summary>
    public static bool IsDuplicateName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (GetSlotName(i) == name) return true;
        }
        return false;
    }

    /// <summary>How many slots are currently in use?</summary>
    public static int UsedSlotCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (HasSlot(i)) count++;
            }
            return count;
        }
    }

    // ── Slot CRUD ────────────────────────────────────────────────

    /// <summary>Create a new save slot with the given name. Returns the slot index, or -1 on failure.</summary>
    public static int CreateSlot(string name)
    {
        name = name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[SaveSlotManager] Slot name cannot be empty.");
            return -1;
        }

        if (IsDuplicateName(name))
        {
            Debug.LogWarning($"[SaveSlotManager] Slot name '{name}' already exists.");
            return -1;
        }

        // Find first empty slot
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!HasSlot(i))
            {
                PlayerPrefs.SetString(GetSlotNameKey(i), name);
                PlayerPrefs.Save();

                // Auto-select if no active slot
                if (!HasActiveSlot)
                    SwitchSlot(i);

                Debug.Log($"[SaveSlotManager] Created slot {i}: '{name}'");
                return i;
            }
        }

        Debug.LogWarning("[SaveSlotManager] All slots are full.");
        return -1;
    }

    /// <summary>Delete a save slot and all its associated data (unlocks).</summary>
    public static void DeleteSlot(int index)
    {
        if (!IsValidIndex(index)) return;
        if (!HasSlot(index)) return;

        string name = GetSlotName(index);

        // Clear unlock data for this slot
        ClearSlotUnlockData(index);

        // Clear slot name
        PlayerPrefs.DeleteKey(GetSlotNameKey(index));

        // If this was the active slot, clear active
        if (ActiveSlotIndex == index)
        {
            ActiveSlotIndex = -1;
            NotifyUnlockManagerReload();
        }

        PlayerPrefs.Save();
        Debug.Log($"[SaveSlotManager] Deleted slot {index}: '{name}'");
    }

    /// <summary>Switch the active save slot. Triggers UnlockManager reload.</summary>
    public static void SwitchSlot(int index)
    {
        if (!IsValidIndex(index)) return;
        if (!HasSlot(index))
        {
            Debug.LogWarning($"[SaveSlotManager] Cannot switch to empty slot {index}.");
            return;
        }

        ActiveSlotIndex = index;
        NotifyUnlockManagerReload();
        Debug.Log($"[SaveSlotManager] Switched to slot {index}: '{GetSlotName(index)}'");
    }

    /// <summary>Clear the active slot selection without deleting data.</summary>
    public static void ClearActiveSlot()
    {
        ActiveSlotIndex = -1;
        NotifyUnlockManagerReload();
    }

    // ── Per-Slot Unlock Key Helper ───────────────────────────────

    /// <summary>
    /// Get the PlayerPrefs key prefix for unlock data in the given slot.
    /// Format: "Save_{slotIndex}_Unlock_"
    /// Used by UnlockManager to construct slot-aware keys.
    /// </summary>
    public static string GetUnlockKeyPrefix(int slotIndex)
    {
        return $"{SlotUnlockKeyPrefix}{slotIndex}_Unlock_";
    }

    /// <summary>Get the unlock key prefix for the currently active slot. Returns empty if no active slot.</summary>
    public static string GetActiveUnlockKeyPrefix()
    {
        if (!HasActiveSlot) return "";
        return GetUnlockKeyPrefix(ActiveSlotIndex);
    }

    // ── Internal ─────────────────────────────────────────────────

    private static bool IsValidIndex(int index)
    {
        return index >= 0 && index < MAX_SLOTS;
    }

    private static string GetSlotNameKey(int index)
    {
        return $"{SlotNameKeyPrefix}{index}{SlotNameKeySuffix}";
    }

    private static void ClearSlotUnlockData(int slotIndex)
    {
        // We need to clear all keys that start with the slot's unlock prefix.
        // PlayerPrefs doesn't support prefix-based deletion, so we iterate
        // through all known CharacterData and clear their keys.
        // This is called from UnlockManager to avoid circular dependency.
        UnlockManager.ClearSlotData(slotIndex);
    }

    private static void NotifyUnlockManagerReload()
    {
        if (UnlockManager.HasInstance)
        {
            UnlockManager.Instance.ReloadUnlockState();
        }
    }
}
