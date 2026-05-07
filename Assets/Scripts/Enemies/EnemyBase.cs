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
    }

    /// <summary>
    /// Set this enemy as an elite with enhanced stats.
    /// </summary>
    public virtual void SetElite()
    {
        _isElite = true;
        _currentHP *= 5f;
        _eliteDamageMultiplier = 2f;

        // Scale up slightly (only 1.05x for elite enemies)
        transform.localScale = transform.localScale * 1.05f;

        // Change color
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
    }

    protected virtual void FixedUpdate()
    {
        if (_cachedPlayer == null) return;
        Vector2 dir = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * Time.fixedDeltaTime);
    }

    protected virtual void Update()
    {
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

        GameEvents.InvokeEnemyDied(this);

        if (DropManager.Instance != null && _data != null)
        {
            // Elite enemies drop more experience and gold
            int expMultiplier = _isElite ? 10 : 1;
            int goldMultiplier = _isElite ? 5 : 1;
            DropManager.Instance.SpawnDrops(transform.position, _data.expValue * expMultiplier, _data.goldValue * goldMultiplier);
        }

        // Pool's resetAction handles ResetForReuse — don't call it here
        PoolManager.Instance.Return<EnemyBase>(_data != null ? _data.enemyName : name, this);
    }

    private void OnDisable()
    {
        _activeEnemies.Remove(this);
        // Unregister is idempotent — safe to call even if Die() already unregistered.
        SpatialGrid.Unregister(this);
    }
}