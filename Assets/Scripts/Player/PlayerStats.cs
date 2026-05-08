using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player stats: HP, EXP, level, and all multipliers consumed by weapons and other systems.
/// Fires GameEvents on damage / level-up / death.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float MaxHP = 100f;
    public float MoveSpeed = 3f;
    public float PickupRange = 2.5f;
    public int Armor;
    public float Luck;
    public float Regen;
    public float DamageMultiplier = 1f;
    public float CooldownMultiplier = 1f;
    public float AreaMultiplier = 1f;
    public int ProjectileBonus;

    [Header("Runtime")]
    [SerializeField] private float _currentHP;
    [SerializeField] private int _level = 1;
    [SerializeField] private float _currentEXP;
    [SerializeField] private int _killCount;
    [SerializeField] private int _gold;
    [SerializeField] private float _totalHealed;
    [SerializeField] private int _eliteKillCount;

    // Magnet buff state
    private float _magnetBuffTimer;
    private float _magnetBuffBonus;
    private float _basePickupRange;

    // Passive tracking: PassiveData → current level (1-based, 0 = not owned)
    private readonly Dictionary<PassiveData, int> _passiveLevels = new Dictionary<PassiveData, int>();

    private float _nextRegenTick;

    public float CurrentHP => _currentHP;
    public int Level => _level;
    public float CurrentEXP => _currentEXP;
    public int KillCount => _killCount;
    public int Gold => _gold;
    public float TotalHealed => _totalHealed;
    public int EliteKillCount => _eliteKillCount;

    /// <summary>EXP needed to reach the next level (quadratic curve).</summary>
    public float EXPToNextLevel => 5 + 3 * _level * _level;

    private void Awake()
    {
        InitializeFromCharacterData();
    }

    /// <summary>Initialize all base stats from the currently selected CharacterData.</summary>
    public void InitializeFromCharacterData()
    {
        var character = GameManager.HasInstance ? GameManager.Instance.SelectedCharacter : null;
        if (character == null) return;

        MaxHP = character.baseHP;
        MoveSpeed = character.moveSpeed;
        PickupRange = character.pickupRange;
        Armor = character.armor;
        Luck = character.luck;
        Regen = character.regen;
        DamageMultiplier = character.damageMultiplier;
        CooldownMultiplier = character.cooldownMultiplier;
        AreaMultiplier = character.areaMultiplier;
        ProjectileBonus = character.projectileBonus;

        _currentHP = MaxHP;
    }

    // ── HP ────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        int actual = Mathf.Max(1, damage - Armor);
        _currentHP -= actual;
        GameEvents.InvokePlayerDamaged(actual);

        if (_currentHP <= 0f)
        {
            _currentHP = 0f;
            GameEvents.InvokePlayerDied();
        }
    }

    public void Heal(float amount)
    {
        _currentHP = Mathf.Min(_currentHP + amount, MaxHP);
        _totalHealed += amount;
        GameEvents.InvokePlayerHealed(amount);
    }

    /// <summary>Clamp CurrentHP to [0, MaxHP]. Call after MaxHP decreases.</summary>
    public void ClampCurrentHP()
    {
        _currentHP = Mathf.Clamp(_currentHP, 0f, MaxHP);
    }

    // ── EXP / Level ──────────────────────────────────────────

    public void AddEXP(int amount)
    {
        _currentEXP += amount;

        while (_currentEXP >= EXPToNextLevel)
        {
            _currentEXP -= EXPToNextLevel;
            _level++;
            GameEvents.InvokePlayerLevelUp(_level);
        }
    }

    /// <summary>0-1 progress towards the next level.</summary>
    public float EXPProgress => _currentEXP / EXPToNextLevel;

    // ── Kill / Gold ───────────────────────────────────────────

    public void AddKill() => _killCount++;
    public void AddEliteKill() => _eliteKillCount++;
    public void AddGold(int amount) => _gold += amount;

    // ── Passive Items ────────────────────────────────────────

    /// <summary>Does the player own this passive?</summary>
    public bool HasPassive(PassiveData passive)
    {
        return _passiveLevels.ContainsKey(passive) && _passiveLevels[passive] > 0;
    }

    /// <summary>Current level of the passive (0 = not owned).</summary>
    public int GetPassiveLevel(PassiveData passive)
    {
        return _passiveLevels.TryGetValue(passive, out int lv) ? lv : 0;
    }

    /// <summary>Apply one level of a passive and increment its tracked level.</summary>
    public void ApplyPassive(PassiveData passive)
    {
        if (passive == null) return;

        int current = GetPassiveLevel(passive);
        if (current >= passive.maxLevel) return;

        PassiveEffect.Apply(this, passive);
        _passiveLevels[passive] = current + 1;
    }

    /// <summary>Remove all levels of a passive and reverse its effects.</summary>
    public void RemovePassive(PassiveData passive)
    {
        if (passive == null) return;

        int current = GetPassiveLevel(passive);
        if (current <= 0) return;

        for (int i = 0; i < current; i++)
        {
            PassiveEffect.Remove(this, passive);
        }
        _passiveLevels.Remove(passive);
    }

    /// <summary>Set passive to a specific level (for debug).</summary>
    public void SetPassiveLevel(PassiveData passive, int level)
    {
        if (passive == null) return;
        level = Mathf.Clamp(level, 0, passive.maxLevel);

        int current = GetPassiveLevel(passive);

        // Remove all existing levels first
        for (int i = 0; i < current; i++)
        {
            PassiveEffect.Remove(this, passive);
        }

        // Apply new level
        for (int i = 0; i < level; i++)
        {
            PassiveEffect.Apply(this, passive);
        }

        if (level > 0)
            _passiveLevels[passive] = level;
        else
            _passiveLevels.Remove(passive);
    }

    public void ApplyMagnetEffect(float duration, float pickupBoost)
    {
        if (_magnetBuffTimer <= 0f)
        {
            _basePickupRange = PickupRange;
            PickupRange += pickupBoost;
        }
        _magnetBuffBonus = pickupBoost;
        _magnetBuffTimer = duration;
    }

    // ── Regen / Buffs ─────────────────────────────────────────

    private void Update()
    {
        if (Regen > 0f)
        {
            _nextRegenTick -= Time.deltaTime;
            if (_nextRegenTick <= 0f)
            {
                Heal(Regen);
                _nextRegenTick = 1f;
            }
        }

        // Magnet buff countdown
        if (_magnetBuffTimer > 0f)
        {
            _magnetBuffTimer -= Time.deltaTime;
            if (_magnetBuffTimer <= 0f)
            {
                PickupRange = _basePickupRange;
                _magnetBuffBonus = 0f;
            }
        }
    }
}