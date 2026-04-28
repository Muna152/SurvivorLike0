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
    private float _maxRange = 30f;
    private Vector2 _origin;
    private float _damageMultiplier = 1f;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
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
        transform.right = _direction;
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        _rb.velocity = _direction * _speed;

        if (Vector2.Distance(_origin, _rb.position) > _maxRange)
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
        _rb.velocity = Vector2.zero;
    }

    private void ReturnToPool()
    {
        ResetForReuse();
        PoolManager.Instance.Return<Projectile>(name, this);
    }
}