using UnityEngine;

/// <summary>
/// Character (playable hero) data definition (ScriptableObject).
/// Defines a character's base stats, starting equipment, and unlock condition.
/// Unlock state is managed at runtime by UnlockManager (not stored on the asset).
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Data/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier (e.g. \"hero\", \"mage\")")]
    public string id;
    public string characterName;
    public Sprite portrait;
    [Tooltip("In-game sprite shown on the Player's SpriteRenderer")]
    public Sprite gameSprite;
    [Tooltip("AnimatorController for this character's animations")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("True if the character's sprite/animation frames face right by default. " +
             "Used by PlayerController to determine flipX direction.")]
    public bool faceRightByDefault = true;
    [TextArea(1, 3)]
    public string description;

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
    [Tooltip("If true, this character is unlocked by default (no condition needed)")]
    public bool isDefaultUnlocked;
}