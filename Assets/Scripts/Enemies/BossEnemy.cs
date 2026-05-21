using UnityEngine;

/// <summary>
/// Base class for all BOSS enemies: multi-phase behavior, health bar integration,
/// enhanced drops, and destroy-on-death (not pooled).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BossEnemy : EnemyBase
{
    // ── Phase System ────────────────────────────────────────────
    [Header("Boss Phases")]
    [SerializeField] protected float[] _phaseThresholds = { 0.7f, 0.3f };

    protected int _currentPhase;
    protected float _maxHP;

    // ── Boss Config ─────────────────────────────────────────────
    [Header("Boss Config")]
    [SerializeField] protected bool _isUnkillable;
    [SerializeField] protected int _expMultiplier = 50;
    [SerializeField] protected int _goldMultiplier = 20;
    [SerializeField] protected float _attackInterval = 3f;

    protected float _attackTimer;
    protected bool _phaseTransitioning;

    // ── Public Accessors ────────────────────────────────────────
    public int CurrentPhase => _currentPhase;
    public float MaxHP => _maxHP;
    public float HealthPercent => _maxHP > 0f ? _currentHP / _maxHP : 0f;
    public string BossName => _data != null ? _data.enemyName : "BOSS";
    public bool IsUnkillable => _isUnkillable;

    // ── Lifecycle ───────────────────────────────────────────────

    public override void Initialize(EnemyData data)
    {
        base.Initialize(data);
        _maxHP = _currentHP;
        _currentPhase = 0;
        _attackTimer = _attackInterval;
        _phaseTransitioning = false;

        // Boss scale — larger than regular enemies
        float bossScale = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bossScaleMultiplier : 2f;
        transform.localScale = Vector3.one * bossScale;

        // Recompute separation radius with the new scale
        ComputeSeparationRadius();

        GameEvents.InvokeBossSpawned(this);
    }

    public override void ResetForReuse()
    {
        base.ResetForReuse();
        _currentPhase = 0;
        _maxHP = 0f;
        _attackTimer = _attackInterval;
        _phaseTransitioning = false;
    }

    public override void OnUpdateTick(float dt)
    {
        base.OnUpdateTick(dt);

        if (_phaseTransitioning) return;

        _attackTimer -= dt;
        if (_attackTimer <= 0f)
        {
            ExecuteAttack();
            _attackTimer = _attackInterval;
        }
    }

    // ── Damage & Death ──────────────────────────────────────────

    public override void TakeDamage(int damage)
    {
        if (_isUnkillable && _currentHP - damage <= 0f)
        {
            // Unkillable bosses stop at 1 HP
            _currentHP = 1f;
            FlashHit();
            GameEvents.InvokeBossHealthChanged(this);
            CheckPhaseTransition();
            return;
        }

        base.TakeDamage(damage);
        GameEvents.InvokeBossHealthChanged(this);
        CheckPhaseTransition();
    }

    protected override void Die()
    {
        if (_isUnkillable) return;

        ActiveEnemies.Remove(this);
        SpatialGrid.Unregister(this);

        // Track kill count on PlayerStats (same as EnemyBase.Die)
        if (CachedPlayerStats != null)
        {
            CachedPlayerStats.AddKill();
            CachedPlayerStats.AddEliteKill();
        }

        GameEvents.InvokeEnemyDied(this);
        GameEvents.InvokeBossDied(this);

        if (DropManager.Instance != null && _data != null)
        {
            DropManager.Instance.SpawnDrops(
                transform.position,
                _data.expValue * _expMultiplier,
                _data.goldValue * _goldMultiplier,
                false, true);
        }

        Destroy(gameObject);
    }

    protected override void OnDisable()
    {
        // Delegates to base class (which does ActiveEnemies.Remove + SpatialGrid.Unregister).
        // Both operations are idempotent, so calling base is safe even if Die() already ran them.
        base.OnDisable();
    }

    // ── Phase System ────────────────────────────────────────────

    protected virtual void CheckPhaseTransition()
    {
        if (_phaseThresholds == null || _currentPhase >= _phaseThresholds.Length) return;

        float hpPercent = _currentHP / _maxHP;
        if (hpPercent <= _phaseThresholds[_currentPhase])
        {
            _currentPhase++;
            OnPhaseChanged(_currentPhase);
        }
    }

    protected virtual void OnPhaseChanged(int newPhase)
    {
        _phaseTransitioning = true;

        // Visual feedback — flash white
        if (_sr != null)
        {
            _sr.color = Color.white;
        }

        // Brief invulnerability during transition
        float transDuration = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bossPhaseTransitionDuration : 0.5f;
        this.InvokeDelayed(transDuration, () =>
        {
            _phaseTransitioning = false;
            if (_sr != null) _sr.color = Color.white;
            // Restore normal color after flash
            this.InvokeDelayed(0.1f, () =>
            {
                if (_sr != null && !_flashing) _sr.color = _originalColor != default ? _originalColor : Color.white;
            });
        });

        // Increase aggression in later phases
        float phaseMultiplier = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.bossAttackIntervalPhaseMultiplier : 0.8f;
        _attackInterval = Mathf.Max(1f, _attackInterval * phaseMultiplier);
    }

    // ── Abstract / Virtual Attack Methods ───────────────────────

    /// <summary>Called every attack interval. Subclasses implement attack patterns.</summary>
    protected abstract void ExecuteAttack();

    // ── Utility ─────────────────────────────────────────────────

    protected PlayerController GetPlayerController()
    {
        return GetPlayer();
    }

    /// <summary>Flash the sprite red on hit (used for unkillable bosses).</summary>
    private void FlashHit()
    {
        if (_sr == null) return;
        if (!_flashing) _originalColor = _sr.color;
        _sr.color = Color.red;
        _flashTimer = 0.1f;
        _flashing = true;
    }
}

/// <summary>
/// Simple delayed callback helper to avoid coroutines.
/// </summary>
public static class BossExtensions
{
    public static void InvokeDelayed(this MonoBehaviour mb, float delay, System.Action action)
    {
        mb.StartCoroutine(DelayedAction(delay, action));
    }

    private static System.Collections.IEnumerator DelayedAction(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
