using UnityEngine;

/// <summary>
/// Weapon database — holds all WeaponData references, accessible via Resources.Load singleton.
/// Path: Resources/Data/WeaponDatabase
/// </summary>
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Data/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    private static WeaponDatabase _instance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            return _instance;
        }
    }

    public WeaponData[] weapons;

    /// <summary>Find weapon by id. Returns null if not found.</summary>
    public WeaponData GetById(string id)
    {
        if (weapons == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null && weapons[i].id == id)
                return weapons[i];
        }
        return null;
    }
}
