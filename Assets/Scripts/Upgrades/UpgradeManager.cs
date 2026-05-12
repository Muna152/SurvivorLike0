using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages level-up upgrades: pauses game, generates 3 options, then resumes on selection.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
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

        // Evolution options (highest priority - always show if available)
        if (_weaponManager != null)
        {
            var evolutionReady = _weaponManager.GetEvolutionReadyWeapons();
            foreach (var w in evolutionReady)
            {
                pool.Add(new WeaponEvolutionOption(w, _weaponManager));
            }
        }

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

        // New weapons (exclude evolution-only weapons)
        if (_weaponManager != null)
        {
            var allWeapons = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.weapons : null;
            if (allWeapons != null)
            {
                foreach (var wd in allWeapons)
                {
                    if (wd == null) continue;
                    if (wd.isEvolutionOnly) continue;
                    if (!_weaponManager.HasWeapon(wd) && _weaponManager.EquippedWeapons.Count < 6)
                    {
                        pool.Add(new NewWeaponOption(wd, _weaponManager));
                    }
                }
            }
        }

        // Passives: only offer if not yet max level
        if (_playerStats != null)
        {
            var allPassives = PassiveDatabase.Instance != null ? PassiveDatabase.Instance.passives : null;
            if (allPassives != null)
            {
                foreach (var pd in allPassives)
                {
                    if (pd == null) continue;
                    int currentLevel = _playerStats.GetPassiveLevel(pd);
                    if (currentLevel < pd.maxLevel)
                    {
                        pool.Add(new PassiveUpgradeOption(pd, _playerStats, currentLevel));
                    }
                }
            }
        }

        // Pick N random non-duplicate options
        // Prioritize evolution options: if any exist, always include at least one
        var result = new List<UpgradeOption>();
        var indices = new HashSet<int>();

        // First, ensure at least one evolution option is included
        var evolutionIndices = new List<int>();
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] is WeaponEvolutionOption)
                evolutionIndices.Add(i);
        }

        if (evolutionIndices.Count > 0)
        {
            int pick = evolutionIndices[Random.Range(0, evolutionIndices.Count)];
            result.Add(pool[pick]);
            indices.Add(pick);
        }

        // Fill remaining slots randomly
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