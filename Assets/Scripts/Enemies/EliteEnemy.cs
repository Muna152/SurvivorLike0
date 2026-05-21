using UnityEngine;

/// <summary>
/// Elite enemy: Enhanced version of regular enemies with higher stats and better rewards.
/// </summary>
public class EliteEnemy : EnemyBase
{
    public override void OnFixedTick(float dt)
    {
        // Elite enemies move slightly faster
        PlayerController player = EnemyBase.GetPlayer();
        if (player == null) return;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * 1.2f * dt);
    }

    public override void Initialize(EnemyData data)
    {
        base.Initialize(data);
        SetElite();
    }

    protected override void Die()
    {
        base.Die();
    }
}
