using UnityEngine;

/// <summary>
/// Base class for all drop items: attracted to the player and auto-collected.
/// </summary>
public class DropBase : MonoBehaviour
{
    public enum DropType { ExpGem, Health, Gold, Chest }

    [SerializeField] private DropType _type;
    [SerializeField] private int _value = 1;

    public DropType Type => _type;
    public int Value => _value;

    private bool _collected;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private float _attractSpeed = 15f;
    private float _collectRadius = 0.5f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void SetValue(int value)
    {
        _value = value;
    }

    private void Update()
    {
        if (_collected) return;

        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        var stats = player.GetComponent<PlayerStats>();
        float pickupRange = stats != null ? stats.PickupRange : 1f;
        Vector2 playerPos = (Vector2)player.transform.position;
        float dist = Vector2.Distance(transform.position, playerPos);

        if (dist <= pickupRange)
        {
            // Accelerate towards player
            Vector2 dir = (playerPos - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * _attractSpeed * Time.deltaTime);

            if (dist <= _collectRadius)
            {
                Collect(stats);
            }
        }
    }

    private void Collect(PlayerStats stats)
    {
        if (_collected) return;
        _collected = true;

        switch (_type)
        {
            case DropType.ExpGem:
                stats.AddEXP(_value);
                break;
            case DropType.Health:
                stats.Heal(_value);
                break;
            case DropType.Gold:
                stats.AddGold(_value);
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
        ResetForReuse();
        string key = _type.ToString();
        PoolManager.Instance.Return<DropBase>(key, this);
    }
}