using UnityEngine;

/// <summary>
/// Death BOSS: Appears at 30 minutes. HP=5000. UNKILLABLE.
/// Phase 1: Fires tracking projectiles that follow the player.
/// Phase 2: Faster projectiles + more projectiles per volley.
/// Phase 3: Near-constant attacks, extreme speed.
/// The player must survive until time runs out — Death cannot be killed.
/// </summary>
public class DeathBoss : BossEnemy
{
    [Header("Death Boss Config")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 3f;
    [SerializeField] private float _projectileDamage = 3f;
    [SerializeField] private int _trackingCount = 3;
    [SerializeField] private float _trackingLifetime = 4f;
    [SerializeField] private float _homingStrength = 2f;

    private int _attackPattern;

    public override void Initialize(EnemyData data)
    {
        _phaseThresholds = new float[] { 0.7f, 0.4f }; // Two phases but never reaches 0
        _expMultiplier = 0; // No rewards for surviving Death
        _goldMultiplier = 0;
        _attackInterval = 2f;
        _isUnkillable = true; // DEATH CANNOT BE KILLED

        base.Initialize(data);
        _attackPattern = 0;
    }

    protected override void FixedUpdate()
    {
        if (_phaseTransitioning) return;

        // Death moves faster than other bosses
        var player = GetPlayerController();
        if (player == null) return;

        float speedMultiplier = _currentPhase >= 2 ? 2f : (_currentPhase >= 1 ? 1.5f : 1f);
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }

    protected override void ExecuteAttack()
    {
        if (_phaseTransitioning) return;

        switch (_attackPattern % GetAttackCount())
        {
            case 0:
                TrackingVolley();
                break;
            case 1:
                if (_currentPhase >= 1)
                    SpreadAttack();
                else
                    TrackingVolley();
                break;
            case 2:
                if (_currentPhase >= 2)
                    TrackingVolley(); // Double tracking in phase 3
                else
                    TrackingVolley();
                break;
        }
        _attackPattern++;
    }

    private int GetAttackCount()
    {
        if (_currentPhase >= 2) return 3;
        if (_currentPhase >= 1) return 2;
        return 1;
    }

    /// <summary>Fire tracking projectiles that home in on the player.</summary>
    private void TrackingVolley()
    {
        if (_projectilePrefab == null) return;
        var player = GetPlayerController();
        if (player == null) return;

        float dmgScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.DamageMultiplier : 1f;
        float damage = _projectileDamage * dmgScale;
        int count = _currentPhase >= 2 ? _trackingCount + 3 : (_currentPhase >= 1 ? _trackingCount + 1 : _trackingCount);

        Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;

        for (int i = 0; i < count; i++)
        {
            // Slight spread around player direction
            float spreadAngle = Random.Range(-30f, 30f);
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float angle = (baseAngle + spreadAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 offset = Random.insideUnitCircle * 1f;
            GameObject proj = Instantiate(_projectilePrefab, (Vector2)transform.position + offset, Quaternion.identity);
            var tp = proj.AddComponent<TrackingProjectile>();
            tp.Initialize(damage, _projectileSpeed, dir, _homingStrength, _trackingLifetime);
        }
    }

    /// <summary>Fire a wide spread of fast projectiles.</summary>
    private void SpreadAttack()
    {
        if (_projectilePrefab == null) return;

        float dmgScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.DamageMultiplier : 1f;
        float damage = _projectileDamage * dmgScale;

        int count = 8;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            var bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.Initialize(damage * 0.5f, _projectileSpeed * 1.5f, dir);
            }
        }
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);

        // Each phase: fire a spread attack immediately
        SpreadAttack();
    }
}

/// <summary>
/// Homing projectile that tracks the player. Added dynamically to BossProjectile objects.
/// </summary>
public class TrackingProjectile : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private Vector2 _direction;
    private float _homingStrength;
    private float _lifetime;
    private float _lifetimeTimer;
    private Rigidbody2D _rb;

    public void Initialize(float damage, float speed, Vector2 direction, float homingStrength, float lifetime)
    {
        _damage = damage;
        _speed = speed;
        _direction = direction.normalized;
        _homingStrength = homingStrength;
        _lifetime = lifetime;
        _lifetimeTimer = lifetime;

        _rb = GetComponent<Rigidbody2D>();

        // Remove BossProjectile component to avoid double movement
        var bp = GetComponent<BossProjectile>();
        if (bp != null) Destroy(bp);
    }

    private void FixedUpdate()
    {
        var player = EnemyBase.GetPlayer();
        if (player != null)
        {
            Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            _direction = Vector2.Lerp(_direction, toPlayer, _homingStrength * Time.fixedDeltaTime).normalized;
        }

        if (_rb != null)
            _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);
        else
            transform.Translate(_direction * _speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        // Rotate to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(Mathf.RoundToInt(_damage));
            Destroy(gameObject);
        }
    }
}
