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
    private bool _projectilePoolRegistered;
    private string _projectilePoolKey;

    public override void Initialize(EnemyData data)
    {
        _phaseThresholds = new float[] { 0.7f, 0.4f }; // Two phases but never reaches 0
        _expMultiplier = 0; // No rewards for surviving Death
        _goldMultiplier = 0;
        _attackInterval = 2f;
        _isUnkillable = true; // DEATH CANNOT BE KILLED

        base.Initialize(data);
        _attackPattern = 0;
        RegisterProjectilePool();
    }

    private void RegisterProjectilePool()
    {
        if (_projectilePoolRegistered || _projectilePrefab == null) return;

        _projectilePoolKey = _projectilePrefab.name.Replace("(Clone)", "").Trim();

        if (PoolManager.HasInstance && PoolManager.Instance.HasPool(_projectilePoolKey))
        {
            _projectilePoolRegistered = true;
            return;
        }

        var prefab = _projectilePrefab;
        PoolManager.Instance.Register<BossProjectile>(
            _projectilePoolKey,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<BossProjectile>();
            },
            bp => { bp.ResetForReuse(); bp.gameObject.SetActive(false); },
            prewarmCount: 40,
            maxSize: 150
        );

        _projectilePoolRegistered = true;
    }

    /// <summary>Get a pooled BossProjectile. Returns null if pool is exhausted.</summary>
    private BossProjectile GetPooledProjectile(Vector2 position)
    {
        if (_projectilePoolRegistered && PoolManager.HasInstance)
        {
            var bp = PoolManager.Instance.Get<BossProjectile>(_projectilePoolKey);
            if (bp != null)
            {
                bp.transform.position = position;
                bp.transform.rotation = Quaternion.identity;
                bp.SetPoolKey(_projectilePoolKey);
                return bp;
            }
        }

        return null;
    }

    public override void OnFixedTick(float dt)
    {
        if (_phaseTransitioning) return;

        // Death moves faster than other bosses
        var player = GetPlayerController();
        if (player == null) return;

        float speedMultiplier = _currentPhase >= 2 ? 2f : (_currentPhase >= 1 ? 1.5f : 1f);
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * speedMultiplier * dt);
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
            var bp = GetPooledProjectile((Vector2)transform.position + offset);
            if (bp != null)
            {
                bp.InitializeHoming(damage, _projectileSpeed, dir, _homingStrength, _trackingLifetime);
            }
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

            var bp = GetPooledProjectile(transform.position);
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
