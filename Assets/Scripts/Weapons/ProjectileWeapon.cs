using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile-type weapon: auto-fires towards the nearest enemy.
/// Supports multiple projectiles in a fan pattern.
/// </summary>
public class ProjectileWeapon : WeaponBase
{
    protected override void Attack()
    {
        if (_data == null || _data.projectilePrefab == null) return;

        var ld = CurrentLevelData;
        if (ld == null) return;

        int count = ld.projectileCount + _playerStats.ProjectileBonus;

        // Find nearest enemy direction
        var enemies = FindObjectsOfType<EnemyBase>();
        if (enemies.Length == 0) return;

        EnemyBase nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(_playerPosition, (Vector2)e.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = e;
            }
        }

        if (nearest == null) return;

        Vector2 baseDir = ((Vector2)nearest.transform.position - _playerPosition).normalized;

        // Fan spread
        float totalAngle = count > 1 ? 30f * (count - 1) : 0f;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? startAngle + (30f * i) : 0f;
            var dir = RotateVector(baseDir, angle);

            var projObj = PoolManager.Instance.Get<Projectile>(_data.projectilePrefab.name);
            if (projObj != null)
            {
                projObj.Launch(_playerPosition, dir, ld, _playerStats.DamageMultiplier);
            }
        }
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}