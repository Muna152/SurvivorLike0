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
    public float PickupRange = 1f;
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

    private float _nextRegenTick;

    public float CurrentHP => _currentHP;
    public int Level => _level;
    public float CurrentEXP => _currentEXP;
    public int KillCount => _killCount;
    public int Gold => _gold;

    /// <summary>EXP needed to reach the next level (quadratic curve).</summary>
    public float EXPToNextLevel => 5 + 5 * _level * _level;

    private void Awake()
    {
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
    public void AddGold(int amount) => _gold += amount;

    // ── Regen ─────────────────────────────────────────────────

    private void Update()
    {
        if (Regen <= 0f) return;

        _nextRegenTick -= Time.deltaTime;
        if (_nextRegenTick <= 0f)
        {
            Heal(Regen);
            _nextRegenTick = 1f;
        }
    }
}