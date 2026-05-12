using UnityEngine;

/// <summary>
/// Mage projectile: Fired by Mage enemies at the player.
/// Pooled via PoolManager — always return to pool instead of Destroy.
/// </summary>
public class MageProjectile : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private Vector2 _direction;
    private float _lifetime = 5f;
    private float _lifetimeTimer;
    private string _poolKey;

    public void Initialize(float damage, float speed, Vector2 direction)
    {
        _damage = damage;
        _speed = speed;
        _direction = direction.normalized;
        _lifetimeTimer = _lifetime;

        // Rotate sprite to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void ResetForReuse()
    {
        _direction = Vector2.right;
        _lifetimeTimer = _lifetime;
    }

    /// <summary>Set the pool key for returning to pool.</summary>
    public void SetPoolKey(string key) => _poolKey = key;

    private void Update()
    {
        // Move projectile
        transform.Translate(_direction * _speed * Time.deltaTime);

        // Check lifetime
        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit player
        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(Mathf.RoundToInt(_damage));
            ReturnToPool();
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
