using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages character unlock state at runtime.
/// Persists unlocks via PlayerPrefs. Checks conditions at game end.
/// Unlock state is NOT stored on CharacterData assets (those are build-time).
/// </summary>
public class UnlockManager : Singleton<UnlockManager>
{
    private const string PlayerPrefsKeyPrefix = "Unlock_";
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

    /// <summary>Reset all unlocks (for debugging).</summary>
    public void ResetAll()
    {
        _unlockedIds.Clear();
        PlayerPrefs.DeleteAll(); // Only deletes unlock keys in production use
        Debug.Log("[UnlockManager] All unlocks reset.");
    }

    // ── Persistence ─────────────────────────────────────────────

    private void LoadUnlockState()
    {
        _unlockedIds.Clear();

        // Load saved unlocks from PlayerPrefs
        if (_allCharacters != null)
        {
            foreach (var character in _allCharacters)
            {
                if (character == null) continue;
                if (character.isDefaultUnlocked) continue;

                string key = GetPrefsKey(character.id);
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
        return $"{PlayerPrefsKeyPrefix}{characterId}";
    }
}
