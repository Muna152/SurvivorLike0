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

    // Cached lists to avoid GC on level-up
    private readonly List<UpgradeOption> _optionPool = new List<UpgradeOption>(16);
    private readonly List<int> _evolutionIndices = new List<int>(4);
    private readonly HashSet<int> _pickedIndices = new HashSet<int>(8);
    private readonly List<UpgradeOption> _resultBuffer = new List<UpgradeOption>(3);

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
        GameManager.Instance.PushPause();
        OnOptionsGenerated?.Invoke(_currentOptions);
    }

    public List<UpgradeOption> GenerateUpgradeOptions(int count)
    {
        // Reuse cached lists
        _optionPool.Clear();
        _evolutionIndices.Clear();
        _pickedIndices.Clear();
        _resultBuffer.Clear();

        // Evolution options (highest priority - always show if available)
        if (_weaponManager != null)
        {
            var evolutionReady = _weaponManager.GetEvolutionReadyWeapons();
            foreach (var w in evolutionReady)
            {
                _optionPool.Add(new WeaponEvolutionOption(w, _weaponManager));
            }
        }

        // Existing weapon upgrades
        if (_weaponManager != null)
        {
            foreach (var w in _weaponManager.EquippedWeapons)
            {
                if (w.CurrentLevel < w.MaxLevel)
                {
                    _optionPool.Add(new WeaponUpgradeOption(w));
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
                        _optionPool.Add(new NewWeaponOption(wd, _weaponManager));
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
                        _optionPool.Add(new PassiveUpgradeOption(pd, _playerStats, currentLevel));
                    }
                }
            }
        }

        // Pick N random non-duplicate options
        // Prioritize evolution options: if any exist, always include at least one
        for (int i = 0; i < _optionPool.Count; i++)
        {
            if (_optionPool[i] is WeaponEvolutionOption)
                _evolutionIndices.Add(i);
        }

        if (_evolutionIndices.Count > 0)
        {
            int pick = _evolutionIndices[Random.Range(0, _evolutionIndices.Count)];
            _resultBuffer.Add(_optionPool[pick]);
            _pickedIndices.Add(pick);
        }

        // Fill remaining slots randomly
        int attempts = 0;
        while (_resultBuffer.Count < count && attempts < _optionPool.Count * 2)
        {
            int idx = Random.Range(0, _optionPool.Count);
            if (_pickedIndices.Add(idx))
            {
                _resultBuffer.Add(_optionPool[idx]);
            }
            attempts++;
        }

        // Return a new list for the caller (queued upgrades need separate lists)
        return new List<UpgradeOption>(_resultBuffer);
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
            // Pop current pause before showing next upgrade (which will push its own pause)
            GameManager.Instance.PopPause();
            ShowUpgrade(_pendingUpgrades.Dequeue());
            return;
        }

        GameManager.Instance.PopPause();
        OnUpgradeComplete?.Invoke();
    }
}