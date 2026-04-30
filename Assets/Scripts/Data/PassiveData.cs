using UnityEngine;

/// <summary>
/// Passive item data definition (ScriptableObject).
/// Defines a passive item's effect per level and stat it modifies.
/// </summary>
[CreateAssetMenu(fileName = "PassiveData", menuName = "Data/PassiveData")]
public class PassiveData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string passiveName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Level Data")]
    public int maxLevel = 5;
    public float effectPerLevel = 0.1f;

    [Header("Stat")]
    public StatType affectedStat;

    [Header("Evolution")]
    public string requiredForEvolutionWeaponId;

    /// <summary>
    /// Which player stat this passive modifies.
    /// </summary>
    public enum StatType
    {
        MoveSpeed,
        PickupRange,
        Armor,
        Luck,
        Regen,
        DamageMultiplier,
        CooldownMultiplier,
        AreaMultiplier,
        ProjectileBonus,
        MaxHP
    }
}