using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages character unlock state at runtime.
/// Persists unlocks via PlayerPrefs with per-slot key prefixes.
/// Unlock state is NOT stored on CharacterData assets (those are build-time).
/// </summary>
public class UnlockManager : Singleton<UnlockManager>
{
    private readonly HashSet<string> _unlockedIds = new HashSet<string>();

    /// <summary>All character data assets available in the game.</summary>
    [Header("Characters")]
    [SerializeField] private CharacterData[] _allCharacters;

    /// <summary>Read-only access to all character definitions.</summary>
    public IReadOnlyList<CharacterData> AllCharacters => _allCharacters;

    protected override void Awake()
    {
        base.Awake();
        LoadCharacterAssets();
        LoadUnlockState();
    }

    /// <summary>Auto-load all CharacterData assets if the serialized array is empty.</summary>
    private void LoadCharacterAssets()
    {
        if (_allCharacters != null && _allCharacters.Length > 0) return;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterData");
        var list = new System.Collections.Generic.List<CharacterData>();
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data != null) list.Add(data);
        }
        _allCharacters = list.ToArray();
#else
        _allCharacters = Resources.FindObjectsOfTypeAll<CharacterData>();
#endif
    }

    // ── Public API ──────────────────────────────────────────────

    /// <summary>Is the character with the given id currently unlocked?</summary>
    public bool IsUnlocked(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return false;

        // Find the character data to check isDefaultUnlocked
        var data = FindCharacter(characterId);
        if (data != null && data.isDefaultUnlocked) return true;

        return _unlockedIds.Contains(characterId);
    }

    /// <summary>Is the character unlocked? (overload accepting CharacterData)</summary>
    public bool IsUnlocked(CharacterData character)
    {
        return character != null && IsUnlocked(character.id);
    }

    /// <summary>Manually unlock a character by id. Saves immediately.</summary>
    public void SetUnlocked(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        // Can only unlock to an active slot
        if (!SaveSlotManager.HasActiveSlot)
        {
            Debug.LogWarning("[UnlockManager] Cannot unlock: no active save slot.");
            return;
        }

        if (_unlockedIds.Add(characterId))
        {
            SaveUnlockState(characterId, true);
            GameEvents.InvokeCharacterUnlocked(characterId);
            Debug.Log($"[UnlockManager] Character unlocked: {characterId}");
        }
    }

    /// <summary>Check all non-default characters against the given stats and unlock any that qualify.</summary>
    public List<CharacterData> CheckUnlocks(PlayerStats stats, float elapsedTime)
    {
        var newlyUnlocked = new List<CharacterData>();

        if (_allCharacters == null) return newlyUnlocked;

        foreach (var character in _allCharacters)
        {
            if (character == null) continue;
            if (character.isDefaultUnlocked) continue;
            if (IsUnlocked(character.id)) continue;

            if (character.unlockCondition.IsUnlocked(stats, elapsedTime))
            {
                SetUnlocked(character.id);
                newlyUnlocked.Add(character);
            }
        }

        return newlyUnlocked;
    }

    /// <summary>Find a CharacterData by its id.</summary>
    public CharacterData FindCharacter(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || _allCharacters == null) return null;

        foreach (var c in _allCharacters)
        {
            if (c != null && c.id == characterId) return c;
        }

        return null;
    }

    /// <summary>Reload unlock state from PlayerPrefs (called when active save slot changes).</summary>
    public void ReloadUnlockState()
    {
        LoadUnlockState();
    }

    /// <summary>Reset all unlocks for the current active slot (for debugging).</summary>
    public void ResetAll()
    {
        _unlockedIds.Clear();

        // Clear unlock data for the active slot only
        if (SaveSlotManager.HasActiveSlot)
        {
            ClearSlotData(SaveSlotManager.ActiveSlotIndex);
        }

        Debug.Log("[UnlockManager] Unlocks reset for current slot.");
    }

    /// <summary>
    /// Clear all unlock PlayerPrefs keys for a given slot index.
    /// Called by SaveSlotManager when deleting a slot.
    /// </summary>
    public static void ClearSlotData(int slotIndex)
    {
        if (!HasInstance) return;

        string prefix = SaveSlotManager.GetUnlockKeyPrefix(slotIndex);
        if (Instance._allCharacters == null) return;

        foreach (var character in Instance._allCharacters)
        {
            if (character == null || character.isDefaultUnlocked) continue;
            string key = prefix + character.id;
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }

    // ── Persistence ─────────────────────────────────────────────

    private void LoadUnlockState()
    {
        _unlockedIds.Clear();

        // If no active slot, only default-unlocked characters are available
        if (!SaveSlotManager.HasActiveSlot) return;

        string prefix = SaveSlotManager.GetActiveUnlockKeyPrefix();

        if (_allCharacters != null)
        {
            foreach (var character in _allCharacters)
            {
                if (character == null) continue;
                if (character.isDefaultUnlocked) continue;

                string key = prefix + character.id;
                if (PlayerPrefs.GetInt(key, 0) == 1)
                {
                    _unlockedIds.Add(character.id);
                }
            }
        }
    }

    private void SaveUnlockState(string characterId, bool unlocked)
    {
        string key = GetPrefsKey(characterId);
        PlayerPrefs.SetInt(key, unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static string GetPrefsKey(string characterId)
    {
        return SaveSlotManager.GetActiveUnlockKeyPrefix() + characterId;
    }
}
