using UnityEngine;

/// <summary>
/// Projectile component: flies in a direction, hits enemies, pierces, then returns to pool.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private int _damage;
    private int _pierce;
    private int _hitCount;
    private float _maxRange = -1f;
    private float MaxRange
    {
        get
        {
            if (_maxRange < 0f)
                _maxRange = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.projectileMaxRange : 30f;
            return _maxRange;
        }
    }
    private Vector2 _origin;
    private float _damageMultiplier = 1f;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private string _poolKey; // cached to avoid string allocation on return

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        // Cache pool key once (strip "(Clone)" suffix added by Unity at runtime)
        _poolKey = gameObject.name.Replace("(Clone)", "").Trim();
    }

    public void Launch(Vector2 origin, Vector2 direction, LevelData data, float damageMultiplier = 1f)
    {
        _origin = origin;
        _direction = direction.normalized;
        _speed = data.speed;
        _damage = data.damage;
        _pierce = data.pierce;
        _hitCount = 0;
        _damageMultiplier = damageMultiplier;

        transform.position = origin;
        transform.up = _direction;

        // Reset velocity for Dynamic RB (pooled objects may carry stale forces)
        if (_rb != null)
            _rb.velocity = Vector2.zero;

        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        // Move via position (Dynamic RB with Continuous detection handles collision)
        _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);

        if ((_rb.position - _origin).sqrMagnitude > MaxRange * MaxRange)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.TakeDamage(Mathf.RoundToInt(_damage * _damageMultiplier));
        _hitCount++;

        if (_hitCount >= _pierce)
        {
            ReturnToPool();
        }
    }

    public void ResetForReuse()
    {
        _hitCount = 0;
        _direction = Vector2.right;
    }

    private void ReturnToPool()
    {
        // Pool's resetAction will call ResetForReuse — don't call it here
        PoolManager.Instance.Return<Projectile>(_poolKey, this);
    }
}