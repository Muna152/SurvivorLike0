using UnityEngine;

/// <summary>
/// Base class for all drop items: attracted to the player and auto-collected.
/// Caches PlayerStats reference to avoid GetComponent every frame.
/// </summary>
public class DropBase : MonoBehaviour
{
    public enum DropType { ExpGem, Health, Gold, Chest }

    [SerializeField] private DropType _type;
    [SerializeField] private int _value = 1;

    public DropType Type => _type;
    public int Value => _value;

    private static PlayerController _cachedPlayer;
    private static PlayerStats _cachedPlayerStats;

    private bool _collected;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private float _attractSpeed = 15f;
    private float _collectRadius = 0.5f;
    private string _poolKey;

    public static void SetPlayerReference(PlayerController player)
    {
        _cachedPlayer = player;
        _cachedPlayerStats = player != null ? player.GetComponent<PlayerStats>() : null;
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        // Cache pool key once (DropType enum name is stable, no "(Clone)" issue)
        _poolKey = _type.ToString();
    }

    public void SetValue(int value)
    {
        _value = value;
    }

    private void Update()
    {
        if (_collected || _cachedPlayer == null || _cachedPlayerStats == null) return;

        float pickupRange = _cachedPlayerStats.PickupRange;
        Vector2 playerPos = (Vector2)_cachedPlayer.transform.position;
        float dist = Vector2.Distance(transform.position, playerPos);

        if (dist <= pickupRange)
        {
            Vector2 dir = (playerPos - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * _attractSpeed * Time.deltaTime);

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
        }

        GameEvents.InvokeDropCollected(this);
        ReturnToPool();
    }

    public virtual void ResetForReuse()
    {
        _collected = false;
    }

    private void ReturnToPool()
    {
        // Pool's resetAction will call ResetForReuse — don't call it here
        PoolManager.Instance.Return<DropBase>(_poolKey, this);
    }
}