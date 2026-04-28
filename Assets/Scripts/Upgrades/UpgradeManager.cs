using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages level-up upgrades: pauses game, generates 3 options, then resumes on selection.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    [Header("Available Weapons")]
    [SerializeField] private WeaponData[] _availableWeapons;

    [Header("Available Passives")]
    [SerializeField] private PassiveData[] _availablePassives;

    private List<UpgradeOption> _currentOptions;
    private PlayerStats _playerStats;
    private PlayerWeaponManager _weaponManager;

    public System.Action<List<UpgradeOption>> OnOptionsGenerated;
    public System.Action OnUpgradeComplete;

    private void Start()
    {
        _playerStats = FindObjectOfType<PlayerStats>();
        _weaponManager = FindObjectOfType<PlayerWeaponManager>();
        GameEvents.OnPlayerLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        GameEvents.OnPlayerLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        Time.timeScale = 0f;
        _currentOptions = GenerateUpgradeOptions(3);
        OnOptionsGenerated?.Invoke(_currentOptions);
    }

    public List<UpgradeOption> GenerateUpgradeOptions(int count)
    {
        var pool = new List<UpgradeOption>();

        // Existing weapon upgrades
        if (_weaponManager != null)
        {
            foreach (var w in _weaponManager.EquippedWeapons)
            {
                if (w.CurrentLevel < w.MaxLevel)
                {
                    pool.Add(new WeaponUpgradeOption(w));
                }
            }
        }

        // New weapons
        if (_weaponManager != null)
        {
            foreach (var wd in _availableWeapons)
            {
                if (!_weaponManager.HasWeapon(wd) && _weaponManager.EquippedWeapons.Count < 6)
                {
                    pool.Add(new NewWeaponOption(wd, _weaponManager));
                }
            }
        }

        // Passives (simplified: each can be picked up to 5 times)
        if (_playerStats != null)
        {
            foreach (var pd in _availablePassives)
            {
                pool.Add(new PassiveUpgradeOption(pd, _playerStats, 0));
            }
        }

        // Pick N random non-duplicate options
        var result = new List<UpgradeOption>();
        var indices = new HashSet<int>();
        int attempts = 0;
        while (result.Count < count && attempts < pool.Count * 2)
        {
            int idx = Random.Range(0, pool.Count);
            if (indices.Add(idx))
            {
                result.Add(pool[idx]);
            }
            attempts++;
        }

        return result;
    }

    public void OnOptionSelected(UpgradeOption option)
    {
        option.Apply();
        Time.timeScale = 1f;
        OnUpgradeComplete?.Invoke();
    }

    /// <summary>Skip the upgrade (optional).</summary>
    public void SkipUpgrade()
    {
        Time.timeScale = 1f;
        OnUpgradeComplete?.Invoke();
    }
}