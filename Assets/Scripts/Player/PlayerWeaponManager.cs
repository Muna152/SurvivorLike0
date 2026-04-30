using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's equipped weapons (max 6).
/// Notifies weapons of player position each frame for auto-attack.
/// Handles weapon evolution when conditions are met.
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    private const int MaxWeapons = 6;
    private readonly List<WeaponBase> _weapons = new List<WeaponBase>();

    [Header("Starting Weapon")]
    [SerializeField] private WeaponData _startingWeapon;

    [Header("All Passives (for evolution lookup)")]
    [SerializeField] private PassiveData[] _allPassives;

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

        WeaponBase weapon = CreateWeaponComponent(child, data);

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

    /// <summary>Check all equipped weapons for evolution conditions and evolve those that qualify.
    /// Returns list of newly evolved weapons.</summary>
    public List<WeaponBase> CheckAndEvolveWeapons()
    {
        var evolved = new List<WeaponBase>();
        var stats = GetComponent<PlayerStats>();
        if (stats == null) return evolved;

        // Iterate backwards since we modify the list
        for (int i = _weapons.Count - 1; i >= 0; i--)
        {
            var weapon = _weapons[i];
            var data = weapon.Data;

            if (!data.canEvolve) continue;
            if (weapon.CurrentLevel < weapon.MaxLevel) continue;
            if (data.evolvedWeapon == null) continue;

            // Check if player has the required passive
            var requiredPassive = FindPassiveById(data.requiredPassiveId);
            if (requiredPassive == null) continue;
            if (!stats.HasPassive(requiredPassive)) continue;

            // Evolve: remove old weapon, equip evolved version
            var evolvedData = data.evolvedWeapon;
            var evolvedName = evolvedData.weaponName;

            // Remove old weapon
            _weapons.RemoveAt(i);
            Destroy(weapon.gameObject);

            // Equip evolved weapon in same slot position
            var child = new GameObject($"Weapon_{evolvedName}");
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;

            WeaponBase evolvedWeapon = CreateWeaponComponent(child, evolvedData);
            evolvedWeapon.Initialize(evolvedData, stats);
            _weapons.Insert(i, evolvedWeapon);

            GameEvents.InvokeWeaponEvolved(evolvedWeapon);
            evolved.Add(evolvedWeapon);

            Debug.Log($"Weapon evolved: {data.weaponName} → {evolvedName}");
        }

        return evolved;
    }

    /// <summary>Get all weapons that can currently evolve (max level + required passive owned).</summary>
    public List<WeaponBase> GetEvolutionReadyWeapons()
    {
        var ready = new List<WeaponBase>();
        var stats = GetComponent<PlayerStats>();
        if (stats == null) return ready;

        foreach (var weapon in _weapons)
        {
            var data = weapon.Data;
            if (!data.canEvolve) continue;
            if (weapon.CurrentLevel < weapon.MaxLevel) continue;
            if (data.evolvedWeapon == null) continue;

            var requiredPassive = FindPassiveById(data.requiredPassiveId);
            if (requiredPassive == null) continue;
            if (!stats.HasPassive(requiredPassive)) continue;

            ready.Add(weapon);
        }

        return ready;
    }

    /// <summary>Find a PassiveData by its id string.</summary>
    public PassiveData FindPassiveById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (_allPassives != null)
        {
            foreach (var p in _allPassives)
            {
                if (p != null && p.id == id) return p;
            }
        }

        // Fallback: search asset database at runtime
        return null;
    }

    /// <summary>Called by PlayerController every FixedUpdate with current position and direction.</summary>
    public void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            _weapons[i].OnPlayerMoved(position, direction);
        }
    }

    private WeaponBase CreateWeaponComponent(GameObject child, WeaponData data)
    {
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
                switch (data.weaponName)
                {
                    case "Holy Light":
                    case "AngelsSong":
                        weapon = child.AddComponent<HolyLight>();
                        break;
                    case "Holy Water":
                    case "UndeadFlood":
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
        return weapon;
    }
}