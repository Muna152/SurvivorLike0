using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all enemies: chase the player, take damage, die & drop loot.
/// Uses HashSet for O(1) tracking and MovePosition for smooth Kinematic movement.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    private static readonly HashSet<EnemyBase> _activeEnemies = new HashSet<EnemyBase>();
    private static PlayerController _cachedPlayer;
    private static PlayerStats _cachedPlayerStats;

    /// <summary>PlayerStats reference for subclasses (e.g. BossEnemy) to use in Die().</summary>
    protected static PlayerStats CachedPlayerStats => _cachedPlayerStats;

    public static HashSet<EnemyBase> ActiveEnemies => _activeEnemies;
    public static int ActiveEnemyCount => _activeEnemies.Count;

    protected EnemyData _data;
    protected float _currentHP;
    protected float _moveSpeed;
    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;

    // Hit flash state (timer instead of coroutine to avoid GC)
    protected float _flashTimer;
    protected Color _originalColor;
    protected bool _flashing;

    // Elite enemy state
    protected bool _isElite;
    protected float _eliteDamageMultiplier;

    // LOD state — far enemies update at lower frequency
    private int _lodFrameOffset;       // Random offset so far enemies don't all update on the same frame
    private bool _isFarLOD;            // Cached: is this enemy in the far LOD tier?
    private float _lodAccumulatedDelta; // Accumulated fixedDeltaTime for skipped frames
    private const float LOD_FAR_DISTANCE = 25f; // Beyond this distance, enemy enters far LOD
    private const int LOD_FAR_INTERVAL = 5;     // Far enemies update every N FixedFrames
    private int _lastLODCheckFrame = -1;         // Frame of last distance check

    // SpatialGrid integration — stores current cell key for O(1) boundary checks.
    // Also used as "registered" flag: non-zero means the enemy is in the grid.
    public long LastCellKey { get; set; }

    public EnemyData Data => _data;
    public float CurrentHP => _currentHP;
    public bool IsElite => _isElite;

    /// <summary>Cache the player reference once. Called by EnemySpawner on Start.</summary>
    public static void SetPlayerReference(PlayerController player)
    {
        _cachedPlayer = player;
        _cachedPlayerStats = player != null ? player.GetComponent<PlayerStats>() : null;
    }

    public static PlayerController GetPlayer()
    {
        return _cachedPlayer;
    }

    /// <summary>
    /// Clear all static state. Call before scene reload to prevent stale references.
    /// </summary>
    public static void ResetStatics()
    {
        _activeEnemies.Clear();
        _cachedPlayer = null;
        _cachedPlayerStats = null;
        SpatialGrid.Clear();
    }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        // Enable trigger detection with other Kinematic bodies (e.g. projectiles)
        if (_rb != null && _rb.bodyType == RigidbodyType2D.Kinematic)
            _rb.useFullKinematicContacts = true;
    }

    public virtual void Initialize(EnemyData data)
    {
        _data = data;

        // Apply difficulty scaling to HP and speed
        float hpScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.HPMultiplier : 1f;
        float speedScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.SpeedMultiplier : 1f;

        _currentHP = data.baseHP * hpScale;
        _moveSpeed = data.moveSpeed * speedScale;

        _activeEnemies.Add(this);
        SpatialGrid.Register(this);

        // LOD: random frame offset to stagger far-enemy updates
        _lodFrameOffset = UnityEngine.Random.Range(0, LOD_FAR_INTERVAL);
        _isFarLOD = false;
        _lodAccumulatedDelta = 0f;
        _lastLODCheckFrame = -1;
    }

    /// <summary>
    /// Set this enemy as an elite with enhanced stats.
    /// </summary>
    public virtual void SetElite()
    {
        _isElite = true;

        var cfg = GameBalanceConfig.Instance;
        float hpMult = cfg != null ? cfg.eliteHpMultiplier : 5f;
        _eliteDamageMultiplier = cfg != null ? cfg.eliteDamageMultiplier : 2f;
        float scaleMult = cfg != null ? cfg.eliteScaleMultiplier : 1.05f;
        float speedMult = cfg != null ? cfg.eliteSpeedMultiplier : 1.2f;

        _currentHP *= hpMult;
        _moveSpeed *= speedMult;

        transform.localScale = transform.localScale * scaleMult;

        if (_sr != null)
        {
            _sr.color = new Color(1f, 0.5f, 0f); // Orange tint
        }
    }

    public virtual void ResetForReuse()
    {
        if (_data != null)
        {
            _currentHP = _data.baseHP;
            _moveSpeed = _data.moveSpeed;
        }

        if (_flashing && _sr != null)
            _sr.color = _originalColor;
        _flashing = false;
        _flashTimer = 0f;

        // Reset elite state — pooled enemies must not carry over elite visuals/stats
        if (_isElite && _sr != null)
            _sr.color = Color.white;
        _isElite = false;
        _eliteDamageMultiplier = 1f;

        _isFarLOD = false;
        _lodAccumulatedDelta = 0f;
        _lastLODCheckFrame = -1;
    }

    protected virtual void FixedUpdate()
    {
        if (_cachedPlayer == null) return;

        // LOD distance check — re-evaluate every 10 frames to avoid per-frame distance computation
        int frame = Time.frameCount;
        if (frame - _lastLODCheckFrame >= 10 || _lastLODCheckFrame < 0)
        {
            float distSq = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).sqrMagnitude;
            _isFarLOD = distSq > LOD_FAR_DISTANCE * LOD_FAR_DISTANCE;
            _lastLODCheckFrame = frame;
        }

        if (_isFarLOD)
        {
            // Far LOD: only move on assigned frame, accumulate delta otherwise
            if (frame % LOD_FAR_INTERVAL != _lodFrameOffset)
            {
                _lodAccumulatedDelta += Time.fixedDeltaTime;
                return;
            }

            // Execute movement with accumulated + current delta
            float dt = _lodAccumulatedDelta + Time.fixedDeltaTime;
            _lodAccumulatedDelta = 0f;
            Vector2 dir = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).normalized;
            _rb.MovePosition(_rb.position + dir * _moveSpeed * dt);
        }
        else
        {
            // Near LOD: full update every frame
            Vector2 dir = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).normalized;
            _rb.MovePosition(_rb.position + dir * _moveSpeed * Time.fixedDeltaTime);
        }
    }

    protected virtual void Update()
    {
        // Far LOD enemies skip visual-only updates (hit flash)
        if (_isFarLOD) return;

        if (_flashing)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                if (_sr != null) _sr.color = _originalColor;
                _flashing = false;
            }
        }
    }

    public virtual void TakeDamage(int damage)
    {
        _currentHP -= damage;

        GameEvents.InvokeEnemyHit(this);
        GameEvents.InvokeEnemyDamaged(this, damage);

        if (_sr != null)
        {
            if (!_flashing) _originalColor = _sr.color;
            _sr.color = Color.red;
            _flashTimer = 0.1f;
            _flashing = true;
        }

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        _activeEnemies.Remove(this);
        SpatialGrid.Unregister(this);

        // Track kill count on PlayerStats
        if (_cachedPlayerStats != null)
        {
            _cachedPlayerStats.AddKill();
            if (_isElite)
                _cachedPlayerStats.AddEliteKill();
        }

        GameEvents.InvokeEnemyDied(this);

        if (DropManager.Instance != null && _data != null)
        {
            // Elite enemies drop more experience and gold
            var cfg = GameBalanceConfig.Instance;
            int expMul = _isElite ? (cfg != null ? cfg.eliteExpMultiplier : 10) : 1;
            int goldMul = _isElite ? (cfg != null ? cfg.eliteGoldMultiplier : 5) : 1;
            DropManager.Instance.SpawnDrops(transform.position, _data.expValue * expMul, _data.goldValue * goldMul, _isElite, false);
        }

        // Pool's resetAction handles ResetForReuse — don't call it here
        PoolManager.Instance.Return<EnemyBase>(_data != null ? _data.enemyName : name, this);
    }

    protected virtual void OnDisable()
    {
        _activeEnemies.Remove(this);
        // Unregister is idempotent — safe to call even if Die() already unregistered.
        SpatialGrid.Unregister(this);
    }
}