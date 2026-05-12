using UnityEngine;

/// <summary>
/// Expanding AoE shockwave ring. Grows from boss position, damages player on contact.
/// Returns to pool after reaching max radius.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class BossShockwave : MonoBehaviour
{
    [SerializeField] private float _expandSpeed = 8f;
    [SerializeField] private float _maxRadius = 6f;
    [SerializeField] private float _damage = 2f;

    private CircleCollider2D _collider;
    private float _currentRadius;
    private bool _hasDamagedPlayer;
    private string _poolKey;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _collider.isTrigger = true;
        _currentRadius = 0.5f;
    }

    /// <summary>
    /// Initialize with custom damage, expand speed, and max radius.
    /// </summary>
    public void Initialize(float damage, float expandSpeed, float maxRadius)
    {
        _damage = damage;
        _expandSpeed = expandSpeed;
        _maxRadius = maxRadius;
        _currentRadius = 0.5f;
        _hasDamagedPlayer = false;

        // Reset collider and scale for pooled objects
        if (_collider != null)
            _collider.radius = _currentRadius;
        transform.localScale = new Vector3(_currentRadius * 2f, _currentRadius * 2f, 1f);
    }

    public void ResetForReuse()
    {
        _currentRadius = 0.5f;
        _hasDamagedPlayer = false;
        _damage = 2f;
        _expandSpeed = 8f;
        _maxRadius = 6f;

        if (_collider != null)
            _collider.radius = _currentRadius;
        transform.localScale = new Vector3(_currentRadius * 2f, _currentRadius * 2f, 1f);
    }

    /// <summary>Set the pool key for returning to pool.</summary>
    public void SetPoolKey(string key) => _poolKey = key;

    private void Update()
    {
        _currentRadius += _expandSpeed * Time.deltaTime;

        // Update collider radius
        if (_collider != null)
            _collider.radius = _currentRadius;

        // Scale visual to match
        float diameter = _currentRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);

        // Return to pool when max radius reached
        if (_currentRadius >= _maxRadius)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasDamagedPlayer) return;

        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(Mathf.RoundToInt(_damage));
            _hasDamagedPlayer = true;
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
