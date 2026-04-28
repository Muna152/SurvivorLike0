using UnityEngine;

/// <summary>
/// Weapon data definition (ScriptableObject).
/// Defines a weapon's stats across all levels and evolution path.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    [TextArea] public string description;
    public WeaponType weaponType;
    public Sprite icon;
    public GameObject projectilePrefab;

    [Header("Level Data")]
    public int maxLevel = 8;
    public LevelData[] levelData;

    [Header("Evolution")]
    public bool canEvolve;
    public string requiredPassiveId;
    public WeaponData evolvedWeapon;

    /// <summary>
    /// Weapon type classification.
    /// </summary>
    public enum WeaponType
    {
        Projectile,
        Orbital,
        Area,
        Auxiliary
    }
}

/// <summary>
/// Per-level stats for a weapon. Indexed 0 = Level 1.
/// </summary>
[System.Serializable]
public class LevelData
{
    public int damage;
    public float cooldown;
    public float area;
    public int projectileCount;
    public int pierce;
    public float speed;
    public float duration;
}