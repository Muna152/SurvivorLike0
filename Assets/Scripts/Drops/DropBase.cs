using UnityEngine;

/// <summary>
/// Base class for all drop items: attracted to the player and auto-collected.
/// Caches PlayerStats reference to avoid GetComponent every frame.
/// After a delay, uncollected items enter "vacuum" mode and attract from far away.
/// </summary>
public class DropBase : MonoBehaviour
{
    public enum DropType { ExpGem, Health, Gold, Chest, Magnet }

    [SerializeField] private DropType _type;
    [SerializeField] private int _value = 1;

    [Header("Vacuum Settings")]
    [SerializeField] private float _vacuumDelay = 0.5f;
    [SerializeField] private float _vacuumRange = 6f;

    [Header("Magnet Effect")]
    [SerializeField] private float _magnetDuration = 10f;
    [SerializeField] private float _magnetPickupBoost = 5f;

    public DropType Type => _type;
    public int Value => _value;
    public float MagnetDuration => _magnetDuration;
    public float MagnetPickupBoost => _magnetPickupBoost;

    private static PlayerController _cachedPlayer;
    private static PlayerStats _cachedPlayerStats;

    private bool _collected;
    private SpriteRenderer _sr;
    private float _attractSpeed = 15f;
    private float _collectRadius = 0.5f;
    private string _poolKey;

    // Vacuum state
    private float _age;
    private bool _vacuuming;
    private float _attractT; // 0→1 easing factor for acceleration

    public static void SetPlayerReference(PlayerController player)
    {
        _cachedPlayer = player;
        _cachedPlayerStats = player != null ? player.GetComponent<PlayerStats>() : null;
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        // Cache pool key once (DropType enum name is stable, no "(Clone)" issue)
        _poolKey = _type.ToString();
    }

    public void SetValue(int value)
    {
        _value = value;
    }

    /// <summary>Override the drop type at spawn time (ensures correctness even if prefab _type is wrong).</summary>
    public void SetType(DropType type)
    {
        _type = type;
        _poolKey = type.ToString();
    }

    /// <summary>Override magnet settings from DropTableData when spawning.</summary>
    public void SetMagnetConfig(float duration, float pickupBoost)
    {
        _magnetDuration = duration;
        _magnetPickupBoost = pickupBoost;
    }

    private void Update()
    {
        if (_collected || _cachedPlayer == null || _cachedPlayerStats == null) return;

        _age += Time.deltaTime;

        float pickupRange = _cachedPlayerStats.PickupRange;
        Vector2 playerPos = (Vector2)_cachedPlayer.transform.position;
        float dist = Vector2.Distance(transform.position, playerPos);

        // Vacuum: after delay, attract from vacuumRange instead of pickupRange
        float attractRange = pickupRange;
        if (!_vacuuming && _age >= _vacuumDelay)
        {
            _vacuuming = true;
        }

        if (_vacuuming)
        {
            attractRange = Mathf.Max(pickupRange, _vacuumRange);
        }

        if (dist <= attractRange)
        {
            // Ease-in acceleration: starts slow, ramps up to full speed
            _attractT = Mathf.Min(1f, _attractT + Time.deltaTime * 2.5f);
            float eased = _attractT * _attractT; // quadratic ease-in
            float baseSpeed = _vacuuming ? _attractSpeed * 2f : _attractSpeed;
            float speed = baseSpeed * eased;

            Vector2 dir = (playerPos - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * speed * Time.deltaTime);

            if (dist <= _collectRadius)
            {
                Collect();
                return;
            }
        }
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        switch (_type)
        {
            case DropType.ExpGem:
                _cachedPlayerStats.AddEXP(_value);
                break;
            case DropType.Health:
                _cachedPlayerStats.Heal(_value);
                break;
            case DropType.Gold:
                _cachedPlayerStats.AddGold(_value);
                break;
            case DropType.Chest:
                var pwm = _cachedPlayer.GetComponent<PlayerWeaponManager>();
                if (pwm != null)
                {
                    pwm.CheckAndEvolveWeapons();
                }
                break;
            case DropType.Magnet:
                _cachedPlayerStats.ApplyMagnetEffect(_magnetDuration, _magnetPickupBoost);
                break;
        }

        GameEvents.InvokeDropCollected(this);
        ReturnToPool();
    }

    public virtual void ResetForReuse()
    {
        _collected = false;
        _age = 0f;
        _vacuuming = false;
        _attractT = 0f;
    }

    private void ReturnToPool()
    {
        // Pool's resetAction will call ResetForReuse — don't call it here
        PoolManager.Instance.Return<DropBase>(_poolKey, this);
    }
}