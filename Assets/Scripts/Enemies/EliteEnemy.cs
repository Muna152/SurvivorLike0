using UnityEngine;

/// <summary>
/// Elite enemy: Enhanced version of regular enemies with higher stats and better rewards.
/// </summary>
public class EliteEnemy : EnemyBase
{
    private float _eliteMoveSpeed; // Cached effective speed to avoid per-frame multiplication

    public override void OnFixedTick(float dt)
    {
        PlayerController player = EnemyBase.GetPlayer();
        if (player == null) return;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _eliteMoveSpeed * dt);
    }

    public override void Initialize(EnemyData data)
    {
        base.Initialize(data);
        SetElite();
        _eliteMoveSpeed = _moveSpeed * 1.2f;
    }

    public override void ResetForReuse()
    {
        base.ResetForReuse();
        _eliteMoveSpeed = 0f;
    }

    protected override void Die()
    {
        base.Die();
    }
}
