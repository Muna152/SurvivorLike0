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
    private readonly Queue<List<UpgradeOption>> _pendingUpgrades = new Queue<List<UpgradeOption>>();
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
        var options = GenerateUpgradeOptions(3);

        if (_currentOptions != null)
        {
            // Already showing an upgrade screen — queue this one
            _pendingUpgrades.Enqueue(options);
            return;
        }

        ShowUpgrade(options);
    }

    private void ShowUpgrade(List<UpgradeOption> options)
    {
        _currentOptions = options;
        Time.timeScale = 0f;
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
        CompleteUpgrade();
    }

    /// <summary>Skip the upgrade (optional).</summary>
    public void SkipUpgrade()
    {
        CompleteUpgrade();
    }

    private void CompleteUpgrade()
    {
        _currentOptions = null;

        if (_pendingUpgrades.Count > 0)
        {
            // Show next queued upgrade
            ShowUpgrade(_pendingUpgrades.Dequeue());
            return;
        }

        Time.timeScale = 1f;
        OnUpgradeComplete?.Invoke();
    }
}