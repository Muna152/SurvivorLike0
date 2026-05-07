using UnityEngine;

/// <summary>
/// Generic projectile for boss attacks. Flies in a direction, damages the player on contact.
/// Supports configurable size, color, and pierce count.
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

        // Rotate to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Reset velocity for pooled objects
        if (_rb != null)
            _rb.velocity = Vector2.zero;
    }

    public void ResetForReuse()
    {
        _hitCount = 0;
        _direction = Vector2.right;
        _lifetimeTimer = _lifetime;
    }

    private void FixedUpdate()
    {
        if (_rb != null)
            _rb.MovePosition(_rb.position + _direction * _speed * Time.fixedDeltaTime);
        else
            transform.Translate(_direction * _speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
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
            _hitCount++;

            if (_hitCount >= _pierce)
            {
                Destroy(gameObject);
            }
        }
    }
}
