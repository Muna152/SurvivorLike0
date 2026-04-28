using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all enemies: chase the player, take damage, die & drop loot.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    protected EnemyData _data;
    protected float _currentHP;
    protected float _moveSpeed;
    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;

    public EnemyData Data => _data;
    public float CurrentHP => _currentHP;

    private static Vector2 PlayerPosition
    {
        get
        {
            var pc = FindObjectOfType<PlayerController>();
            return pc != null ? (Vector2)pc.transform.position : Vector2.zero;
        }
    }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    public virtual void Initialize(EnemyData data)
    {
        _data = data;
        _currentHP = data.baseHP;
        _moveSpeed = data.moveSpeed;
    }

    public virtual void ResetForReuse()
    {
        if (_data != null)
        {
            _currentHP = _data.baseHP;
            _moveSpeed = _data.moveSpeed;
        }
    }

    protected virtual void FixedUpdate()
    {
        Vector2 dir = (PlayerPosition - (Vector2)transform.position).normalized;
        _rb.velocity = dir * _moveSpeed;
    }

    public virtual void TakeDamage(int damage)
    {
        _currentHP -= damage;
        if (_sr != null) StartCoroutine(FlashCoroutine());

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        GameEvents.InvokeEnemyDied(this);

        // Drop loot via DropManager
        var dm = FindObjectOfType<DropManager>();
        if (dm != null && _data != null)
        {
            dm.SpawnDrops(transform.position, _data.expValue, _data.goldValue);
        }

        // Return to pool
        PoolManager.Instance.Return<EnemyBase>(_data != null ? _data.enemyName : name, this);
    }

    private IEnumerator FlashCoroutine()
    {
        if (_sr == null) yield break;
        Color orig = _sr.color;
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _sr.color = orig;
    }
}