using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's equipped weapons (max 6).
/// Notifies weapons of player position each frame for auto-attack.
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    private const int MaxWeapons = 6;
    private readonly List<WeaponBase> _weapons = new List<WeaponBase>();

    [Header("Starting Weapon")]
    [SerializeField] private WeaponData _startingWeapon;

    public IReadOnlyList<WeaponBase> EquippedWeapons => _weapons;

    private void Start()
    {
        if (_startingWeapon != null)
        {
            EquipWeapon(_startingWeapon);
        }
    }

    /// <summary>Equip a new weapon from data. Returns the created WeaponBase or null.</summary>
    public WeaponBase EquipWeapon(WeaponData data)
    {
        if (data == null) return null;
        if (_weapons.Count >= MaxWeapons) return null;
        if (HasWeapon(data)) return null;

        var child = new GameObject($"Weapon_{data.weaponName}");
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;

        WeaponBase weapon = null;
        switch (data.weaponType)
        {
            case WeaponData.WeaponType.Projectile:
                weapon = child.AddComponent<ProjectileWeapon>();
                break;
            case WeaponData.WeaponType.Orbital:
                weapon = child.AddComponent<OrbitalWeapon>();
                break;
            case WeaponData.WeaponType.Area:
                // Determine which area weapon to create based on weapon name
                switch (data.weaponName)
                {
                    case "Holy Light":
                        weapon = child.AddComponent<HolyLight>();
                        break;
                    case "Holy Water":
                        weapon = child.AddComponent<HolyWater>();
                        break;
                    default:
                        weapon = child.AddComponent<AreaWeapon>();
                        break;
                }
                break;
            case WeaponData.WeaponType.Auxiliary:
                weapon = child.AddComponent<AuxiliaryWeapon>();
                break;
            default:
                weapon = child.AddComponent<ProjectileWeapon>();
                break;
        }

        var stats = GetComponent<PlayerStats>();
        weapon.Initialize(data, stats);
        _weapons.Add(weapon);
        return weapon;
    }

    /// <summary>Upgrade the weapon at the given index.</summary>
    public void UpgradeWeapon(int index)
    {
        if (index >= 0 && index < _weapons.Count)
        {
            _weapons[index].Upgrade();
        }
    }

    /// <summary>Find weapon index by data. Returns -1 if not found.</summary>
    public int FindWeaponIndex(WeaponData data)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Data == data) return i;
        }
        return -1;
    }

    public bool HasWeapon(WeaponData data) => FindWeaponIndex(data) >= 0;

    /// <summary>Called by PlayerController every FixedUpdate with current position and direction.</summary>
    public void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            _weapons[i].OnPlayerMoved(position, direction);
        }
    }
}