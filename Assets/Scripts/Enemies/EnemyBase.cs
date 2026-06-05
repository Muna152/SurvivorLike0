using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for centralized enemy ticking. EnemyManager drives all updates
/// through this interface, eliminating per-MonoBehaviour Update/FixedUpdate overhead.
/// </summary>
public interface IEnemyTick
{
    void OnFixedTick(float dt);
    void OnUpdateTick(float dt);
}

/// <summary>
/// Base class for all enemies: chase the player, take damage, die & drop loot.
/// Uses HashSet for O(1) tracking and MovePosition for smooth Kinematic movement.
/// Update logic is driven centrally by EnemyManager via IEnemyTick.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour, IEnemyTick
{
    private static readonly HashSet<EnemyBase> _activeEnemies = new HashSet<EnemyBase>();
    private static PlayerController _cachedPlayer;
    private static PlayerStats _cachedPlayerStats;

    /// <summary>True while EnemyManager is iterating the activeEnemies set.</summary>
    internal static bool IsIterating { get; set; }

    // Enemies that die during iteration — removed after the loop completes
    private static readonly List<EnemyBase> _pendingRemoves = new List<EnemyBase>();
    // Enemies that spawn during iteration — added after the loop completes
    private static readonly List<EnemyBase> _pendingAdds = new List<EnemyBase>();

    /// <summary>PlayerStats reference for subclasses (e.g. BossEnemy) to use in Die().</summary>
    protected static PlayerStats CachedPlayerStats => _cachedPlayerStats;

    public static HashSet<EnemyBase> ActiveEnemies => _activeEnemies;
    public static int ActiveEnemyCount => _activeEnemies.Count;

    protected EnemyData _data;
    protected float _currentHP;
    protected float _moveSpeed;
    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;
    protected Animator _animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private Vector2 _lastMoveDir;

    // Separation radius for enemy-enemy collision (computed from sprite or data)
    private float _separationRadius;

    // Hit flash state (timer instead of coroutine to avoid GC)
    protected float _flashTimer;
    protected Color _originalColor;
    protected bool _flashing;

    // Elite enemy state
    protected bool _isElite;
    protected float _eliteDamageMultiplier;

    // Slow state
    protected float _slowMultiplier = 1f;
    protected float _slowTimer;

    // LOD state — far enemies update at lower frequency
    private int _lodFrameOffset;       // Random offset so far enemies don't all update on the same frame
    private bool _isFarLOD;            // Cached: is this enemy in the far LOD tier?
    private float _lodAccumulatedDelta; // Accumulated fixedDeltaTime for skipped frames
    /// <summary>Public accessor for far LOD state (used by EnemyManager separation pass).</summary>
    public bool IsFarLOD => _isFarLOD;
    private const float LOD_FAR_DISTANCE = 25f; // Beyond this distance, enemy enters far LOD
    private const int LOD_FAR_INTERVAL = 5;     // Far enemies update every N FixedFrames
    private int _lastLODCheckFrame = -1;         // Frame of last distance check

    // SpatialGrid integration — stores current cell key for O(1) boundary checks.
    // Also used as "registered" flag: non-zero means the enemy is in the grid.
    public long LastCellKey { get; set; }

    public EnemyData Data => _data;
    public float CurrentHP => _currentHP;
    public bool IsElite => _isElite;
    public float SeparationRadius => _separationRadius;

    /// <summary>Cache the player reference once. Called by EnemySpawner on Start.</summary>
    public static void SetPlayerReference(PlayerController player)
    {
        _cachedPlayer = player;
        _cachedPlayerStats = player != null ? player.GetComponent<PlayerStats>() : null;
    }

    /// <summary>
    /// Add an enemy to the active set. If currently iterating, defers addition.
    /// </summary>
    private static void SafeAdd(EnemyBase enemy)
    {
        if (IsIterating)
        {
            _pendingAdds.Add(enemy);
        }
        else
        {
            _activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Remove an enemy from the active set. If currently iterating, defers removal.
    /// </summary>
    private static void SafeRemove(EnemyBase enemy)
    {
        if (IsIterating)
        {
            _pendingRemoves.Add(enemy);
        }
        else
        {
            _activeEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// Flush any deferred additions and removals after iteration completes.
    /// Called by EnemyManager after each tick loop.
    /// </summary>
    public static void FlushPendingRemoves()
    {
        for (int i = 0; i < _pendingAdds.Count; i++)
            _activeEnemies.Add(_pendingAdds[i]);
        _pendingAdds.Clear();

        for (int i = 0; i < _pendingRemoves.Count; i++)
            _activeEnemies.Remove(_pendingRemoves[i]);
        _pendingRemoves.Clear();
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
        _animator = GetComponent<Animator>();

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

        SafeAdd(this);
        SpatialGrid.Register(this);

        // Compute separation radius from data or sprite
        ComputeSeparationRadius();

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

        // Recompute separation radius after scale change
        ComputeSeparationRadius();

        if (_sr != null)
        {
            _sr.color = new Color(1f, 0.5f, 0f); // Orange tint
        }
    }

    /// <summary>
    /// Compute the separation radius from EnemyData, non-trigger collider, or sprite bounds.
    /// Used by EnemyManager's separation pass to prevent enemy overlap.
    /// </summary>
    protected virtual void ComputeSeparationRadius()
    {
        // Priority 1: Explicit data override
        if (_data != null && _data.separationRadius > 0f)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
            _separationRadius = _data.separationRadius * scale;
            return;
        }

        // Priority 2: Non-trigger CircleCollider2D (e.g., boss hitbox colliders)
        var colliders = GetComponents<Collider2D>();
        float bestColliderRadius = -1f;
        for (int i = 0; i < colliders.Length; i++)
        {
            var c = colliders[i];
            if (!c.isTrigger && c is CircleCollider2D cc && cc.radius > 0f)
            {
                if (bestColliderRadius < 0f || cc.radius < bestColliderRadius)
                    bestColliderRadius = cc.radius;
            }
        }

        if (bestColliderRadius > 0f)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
            _separationRadius = bestColliderRadius * scale;
            return;
        }

        // Priority 3: Sprite bounds (works well for regular enemies with tight sprites)
        if (_sr != null && _sr.sprite != null)
        {
            var s = _sr.sprite;
            float w = s.rect.width / s.pixelsPerUnit;
            float h = s.rect.height / s.pixelsPerUnit;
            float maxDim = Mathf.Max(w, h) * 0.5f;
            float scale = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
            _separationRadius = maxDim * scale;
        }
        else
        {
            _separationRadius = 0.5f;
        }
    }

    /// <summary>
    /// Apply a separation push vector. Called by EnemyManager's separation pass.
    /// </summary>
    public void ApplySeparationPush(Vector2 push)
    {
        if (_rb != null)
            _rb.MovePosition(_rb.position + push);
    }

    /// <summary>
    /// Apply a slow effect. Only overrides if the new slow is stronger or lasts longer.
    /// factor: speed multiplier (e.g. 0.7 = 30% slow). duration: seconds.
    /// </summary>
    public void ApplySlow(float factor, float duration)
    {
        if (factor < _slowMultiplier || duration > _slowTimer)
        {
            _slowMultiplier = factor;
            _slowTimer = duration;
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

        // Reset slow state
        _slowMultiplier = 1f;
        _slowTimer = 0f;

        // Reset animation state
        _lastMoveDir = Vector2.zero;
        if (_animator != null)
            _animator.SetFloat(SpeedHash, 0f);

        _isFarLOD = false;
        _lodAccumulatedDelta = 0f;
        _lastLODCheckFrame = -1;
    }

    /// <summary>
    /// Centralized physics tick driven by EnemyManager. Replaces MonoBehaviour.FixedUpdate().
    /// </summary>
    public virtual void OnFixedTick(float dt)
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

        Vector2 dir = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).normalized;
        bool isMoving = dir.sqrMagnitude > 0.01f;

        if (_isFarLOD)
        {
            // Far LOD: only move on assigned frame, accumulate delta otherwise
            if (frame % LOD_FAR_INTERVAL != _lodFrameOffset)
            {
                _lodAccumulatedDelta += dt;
                return;
            }

            // Execute movement with accumulated + current delta
            float accumulatedDt = _lodAccumulatedDelta + dt;
            float effectiveSpeedFar = _moveSpeed * _slowMultiplier;
            _lodAccumulatedDelta = 0f;
            _rb.MovePosition(_rb.position + dir * effectiveSpeedFar * accumulatedDt);
        }
        else
        {
            // Near LOD: full update every frame
            float effectiveSpeed = _moveSpeed * _slowMultiplier;
            _rb.MovePosition(_rb.position + dir * effectiveSpeed * dt);
        }

        // Track movement direction for animation and sprite flipping
        if (isMoving)
            _lastMoveDir = dir;

        if (_animator != null)
            _animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
    }

    /// <summary>
    /// Centralized per-frame tick driven by EnemyManager. Replaces MonoBehaviour.Update().
    /// </summary>
    public virtual void OnUpdateTick(float dt)
    {
        // Flip sprite based on horizontal movement direction
        if (_sr != null && _lastMoveDir.x != 0f)
            _sr.flipX = _lastMoveDir.x < 0f;

        // Slow timer countdown
        if (_slowTimer > 0f)
        {
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
                _slowTimer = 0f;
            }
        }

        // Far LOD enemies skip visual-only updates (hit flash)
        if (_isFarLOD) return;

        if (_flashing)
        {
            _flashTimer -= dt;
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
        SafeRemove(this);
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
        SafeRemove(this);
        // Unregister is idempotent — safe to call even if Die() already unregistered.
        SpatialGrid.Unregister(this);
    }
}