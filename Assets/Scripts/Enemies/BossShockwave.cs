using UnityEngine;

/// <summary>
/// Expanding AoE shockwave ring. Grows from boss position, damages player on contact.
/// Destroys itself after reaching max radius.
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
    }

    private void Update()
    {
        _currentRadius += _expandSpeed * Time.deltaTime;

        // Update collider radius
        if (_collider != null)
            _collider.radius = _currentRadius;

        // Scale visual to match
        float diameter = _currentRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);

        // Destroy when max radius reached
        if (_currentRadius >= _maxRadius)
        {
            Destroy(gameObject);
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
}
