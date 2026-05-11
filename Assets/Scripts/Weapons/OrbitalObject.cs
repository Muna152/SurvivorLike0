using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Orbital object component: rotates around a center position and damages enemies on contact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OrbitalObject : MonoBehaviour
{
    private Vector2 _centerPosition;
    private float _radius;
    private float _rotationSpeed;
    private float _damage;
    private float _currentAngle;
    private float _hitCooldown = -1f;
    private float HitCooldown
    {
        get
        {
            if (_hitCooldown < 0f)
                _hitCooldown = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.orbitalHitCooldown : 0.15f;
            return _hitCooldown;
        }
    }
    private float _hitTimer;
    private HashSet<EnemyBase> _hitEnemies;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private string _poolKey;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _hitEnemies = new HashSet<EnemyBase>();
        
        // Cache pool key once
        _poolKey = gameObject.name.Replace("(Clone)", "").Trim();
        
        // Setup rigidbody for collision detection
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Setup collider
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<CircleCollider2D>();
        }
        if (_collider is CircleCollider2D circleCollider)
        {
            circleCollider.isTrigger = true;
        }
    }

    public void Initialize(Vector2 centerPosition, float radius, float damage)
    {
        _centerPosition = centerPosition;
        _radius = radius;
        _damage = damage;
        _currentAngle = 0f;
        _hitTimer = 0f;
        _hitEnemies.Clear();
        
        UpdatePosition();
        gameObject.SetActive(true);
    }

    public void SetCenterPosition(Vector2 centerPosition)
    {
        _centerPosition = centerPosition;
        UpdatePosition();
    }

    public void SetAngle(float angle)
    {
        _currentAngle = angle;
        UpdatePosition();
    }

    public void UpdateProperties(float radius, float rotationSpeed, float damage)
    {
        _radius = radius;
        _rotationSpeed = rotationSpeed;
        _damage = damage;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float radians = _currentAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _radius;
        _rb.MovePosition(_centerPosition + offset);
        
        // Rotate the sprite to face the center
        Vector2 toCenter = (_centerPosition - (Vector2)_rb.position).normalized;
        float angle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void FixedUpdate()
    {
        // Update hit cooldown timer
        if (_hitTimer > 0f)
        {
            _hitTimer -= Time.fixedDeltaTime;
            if (_hitTimer <= 0f)
            {
                _hitEnemies.Clear();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;

        // Skip if already hit this enemy within cooldown window
        if (_hitEnemies.Contains(enemy)) return;

        // Deal damage
        enemy.TakeDamage(Mathf.RoundToInt(_damage));

        // Add to hit set; start cooldown timer on first hit
        _hitEnemies.Add(enemy);
        if (_hitTimer <= 0f)
            _hitTimer = HitCooldown;
    }

    public void ResetForReuse()
    {
        _hitEnemies.Clear();
        _hitTimer = 0f;
    }

    private void OnDisable()
    {
        _hitEnemies.Clear();
        _hitTimer = 0f;
    }
}
