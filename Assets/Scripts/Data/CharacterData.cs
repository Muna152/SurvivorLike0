using UnityEngine;

/// <summary>
/// Character (playable hero) data definition (ScriptableObject).
/// Defines a character's base stats and starting equipment.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Data/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public Sprite portrait;

    [Header("Base Stats")]
    public float baseHP = 100f;
    public float moveSpeed = 5f;
    public float pickupRange = 1f;
    public int armor;
    public float luck;
    public float regen;
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public float areaMultiplier = 1f;
    public int projectileBonus;

    [Header("Starting Equipment")]
    public WeaponData startingWeapon;
    public string specialPassiveId;

    [Header("Unlock")]
    public UnlockCondition unlockCondition;
    [Tooltip("Whether this character is currently unlocked")]
    public bool unlocked = true; // Default true for the starting character
}