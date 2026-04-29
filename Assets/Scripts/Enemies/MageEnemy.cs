using UnityEngine;

/// <summary>
/// Mage enemy: stops at attack range and fires projectiles at the player.
/// Falls back to chase behavior when out of range.
/// </summary>
public class MageEnemy : EnemyBase
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _fireRate = 2f;
    [SerializeField] private float _projectileSpeed = 4f;

    private float _fireCooldown;

    public override void Initialize(EnemyData data)
    {
        base.Initialize(data);
        _fireCooldown = _fireRate;
    }

    public override void ResetForReuse()
    {
        base.ResetForReuse();
        _fireCooldown = _fireRate;
    }

    protected override void FixedUpdate()
    {
        var player = GetPlayer();
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist > _attackRange)
        {
            Vector2 dir = toPlayer.normalized;
            _rb.MovePosition(_rb.position + dir * _moveSpeed * Time.fixedDeltaTime);
        }
    }

    protected override void Update()
    {
        base.Update();

        var player = GetPlayer();
        if (player == null || _projectilePrefab == null) return;

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= _attackRange)
            {
                FireProjectile();
            }
            _fireCooldown = _fireRate;
        }
    }

    private void FireProjectile()
    {
        var player = GetPlayer();
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        GameObject proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
        var mageProj = proj.GetComponent<MageProjectile>();
        if (mageProj != null)
        {
            mageProj.Initialize(_data.damage, _projectileSpeed, dir);
        }
    }
}
