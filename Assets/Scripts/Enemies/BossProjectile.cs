using UnityEngine;

/// <summary>
/// Generic projectile for boss attacks. Flies in a direction, damages the player on contact.
/// Supports configurable size, color, pierce count, and optional homing behavior.
/// Pooled via PoolManager — always return to pool instead of Destroy.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private Vector2 _direction;
    private float _lifetime = 6f;
    private float _lifetimeTimer;
    private int _pierce;
    private int _hitCount;
    private Rigidbody2D _rb;

    // Homing (tracking) fields
    private bool _isHoming;
    private float _homingStrength;
    private string _poolKey;

    public float Damage => _damage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null && _rb.bodyType == RigidbodyType2D.Kinematic)
            _rb.useFullKinematicContacts = true;
    }

    /// <summary>
    /// Initialize the projectile with damage, speed, and direction.
    /// </summary>
    public void Initialize(float damage, float speed, Vector2 direction, int pierce = 1)
    {
        _damage = damage;
        _speed = speed;
        _direction = direction.normalized;
        _pierce = pierce;
        _hitCount = 0;
        _lifetimeTimer = _lifetime;
        _isHoming = false;
        _homingStrength = 0f;

        // Rotate to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Reset velocity for pooled objects
        if (_rb != null)
            _rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// Initialize as a homing (tracking) projectile.
    /// </summary>
    public void InitializeHoming(float damage, float speed, Vector2 direction, float homingStrength, float lifetime, int pierce = 1)
    {
        _damage = damage;
        _speed = speed;
        _direction = direction.normalized;
        _pierce = pierce;
        _hitCount = 0;
        _isHoming = true;
        _homingStrength = homingStrength;
        _lifetime = lifetime;
        _lifetimeTimer = lifetime;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (_rb != null)
            _rb.velocity = Vector2.zero;
    }

    public void ResetForReuse()
    {
        _hitCount = 0;
        _direction = Vector2.right;
        _lifetimeTimer = _lifetime;
        _isHoming = false;
        _homingStrength = 0f;

        if (_rb != null)
            _rb.velocity = Vector2.zero;
    }

    /// <summary>Set the pool key for returning to pool.</summary>
    public void SetPoolKey(string key) => _poolKey = key;

    private void FixedUpdate()
    {
        // Homing: adjust direction toward player
        if (_isHoming)
        {
            var player = EnemyBase.GetPlayer();
            if (player != null)
            {
                Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                _direction = Vector2.Lerp(_direction, toPlayer, _homingStrength * Time.fixedDeltaTime).normalized;
            }
        }

        if (_rb != null)
            _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);
        else
            transform.Translate(_direction * _speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        // Homing: rotate to face direction
        if (_isHoming)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(Mathf.RoundToInt(_damage));
            _hitCount++;

            if (_hitCount >= _pierce)
            {
                ReturnToPool();
            }
        }
    }

    private void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(_poolKey) && PoolManager.HasInstance && PoolManager.Instance.HasPool(_poolKey))
        {
            PoolManager.Instance.Return(_poolKey, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
