using UnityEngine;

/// <summary>
/// Mage enemy: stops at attack range and fires projectiles at the player.
/// Falls back to chase behavior when out of range.
/// </summary>
public class MageEnemy : EnemyBase
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _fireRate = 2f;
    [SerializeField] private float _projectileSpeed = 4f;

    private float _fireCooldown;
    private bool _projectilePoolRegistered;
    private string _projectilePoolKey;
    private DifficultyManager _difficultyManager; // Cached to avoid repeated Singleton lock

    public override void Initialize(EnemyData data)
    {
        base.Initialize(data);
        _fireCooldown = _fireRate;
        _difficultyManager = DifficultyManager.HasInstance ? DifficultyManager.Instance : null;
        RegisterProjectilePool();
    }

    public override void ResetForReuse()
    {
        base.ResetForReuse();
        _fireCooldown = _fireRate;
        _difficultyManager = DifficultyManager.HasInstance ? DifficultyManager.Instance : null;
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
        PoolManager.Instance.Register<MageProjectile>(
            _projectilePoolKey,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<MageProjectile>();
            },
            mp => { mp.ResetForReuse(); mp.gameObject.SetActive(false); },
            prewarmCount: 15,
            maxSize: 60
        );

        _projectilePoolRegistered = true;
    }

    public override void OnFixedTick(float dt)
    {
        var player = GetPlayer();
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist > _attackRange)
        {
            Vector2 dir = toPlayer.normalized;
            _rb.MovePosition(_rb.position + dir * _moveSpeed * dt);
        }
    }

    public override void OnUpdateTick(float dt)
    {
        base.OnUpdateTick(dt);

        var player = GetPlayer();
        if (player == null || _projectilePrefab == null) return;

        _fireCooldown -= dt;
        if (_fireCooldown <= 0f)
        {
            float distSq = ((Vector2)transform.position - (Vector2)player.transform.position).sqrMagnitude;
            if (distSq <= _attackRange * _attackRange)
            {
                FireProjectile();
            }
            _fireCooldown = _fireRate;
        }
    }

    private void FireProjectile()
    {
        var player = GetPlayer();
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;

        MageProjectile mageProj = null;
        if (_projectilePoolRegistered && PoolManager.HasInstance)
        {
            mageProj = PoolManager.Instance.Get<MageProjectile>(_projectilePoolKey);
        }

        if (mageProj == null)
        {
            // Fallback: instantiate directly
            GameObject proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            mageProj = proj.GetComponent<MageProjectile>();
        }
        else
        {
            mageProj.transform.position = transform.position;
            mageProj.transform.rotation = Quaternion.identity;
            mageProj.SetPoolKey(_projectilePoolKey);
        }

        if (mageProj != null)
        {
            float dmgScale = _difficultyManager != null ? _difficultyManager.DamageMultiplier : 1f;
            mageProj.Initialize(_data.damage * dmgScale, _projectileSpeed, dir);
        }
    }
}
