using UnityEngine;

/// <summary>
/// Abstract base class for all weapons.
/// Manages cooldown timer and auto-attack cycle.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _data;
    protected int _currentLevel = 1;
    protected float _cooldownTimer;
    protected PlayerStats _playerStats;
    protected Vector2 _playerPosition;
    protected Vector2 _playerDirection;

    public WeaponData Data => _data;
    public int CurrentLevel => _currentLevel;
    public int MaxLevel => _data != null ? _data.maxLevel : 1;

    public virtual void Initialize(WeaponData data, PlayerStats stats)
    {
        _data = data;
        _playerStats = stats;
        _currentLevel = 1;
        _cooldownTimer = 0f;
    }

    public virtual void Upgrade()
    {
        if (_data != null && _currentLevel < _data.maxLevel)
        {
            _currentLevel++;
        }
    }

    /// <summary>Set weapon level directly (for debug). Clamps to [1, maxLevel].</summary>
    public void SetLevel(int level)
    {
        if (_data == null) return;
        _currentLevel = Mathf.Clamp(level, 1, _data.maxLevel);
    }

    /// <summary>Called by PlayerWeaponManager every physics frame.</summary>
    public virtual void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        _playerPosition = position;
        _playerDirection = direction;
    }

    protected LevelData CurrentLevelData
    {
        get
        {
            if (_data == null || _data.levelData == null) return null;
            int idx = Mathf.Clamp(_currentLevel - 1, 0, _data.levelData.Length - 1);
            return _data.levelData[idx];
        }
    }

    protected virtual void Update()
    {
        if (_playerStats == null || _data == null) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            Attack();
            var ld = CurrentLevelData;
            if (ld != null)
                _cooldownTimer = ld.cooldown * _playerStats.CooldownMultiplier;
        }
    }

    protected abstract void Attack();
}