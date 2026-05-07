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
    [Tooltip("Only used when weaponType is Area. Determines the AreaWeapon subclass.")]
    public AreaSubType areaSubType = AreaSubType.None;
    public Sprite icon;
    public GameObject projectilePrefab;

    [Header("Projectile Settings")]
    [Tooltip("Angle in degrees between each projectile in a fan spread.")]
    public float spreadAngle = 30f;

    [Header("Level Data")]
    public int maxLevel = 8;
    public LevelData[] levelData;

    [Header("Evolution")]
    public bool canEvolve;
    public string requiredPassiveId;
    public WeaponData evolvedWeapon;

    [Tooltip("If true, this weapon can only be obtained through evolution, not from the upgrade pool.")]
    public bool isEvolutionOnly;

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

    /// <summary>
    /// Sub-type for Area weapons. Determines which component is created.
    /// </summary>
    public enum AreaSubType
    {
        None,
        HealingAura,
        DamagePuddle
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