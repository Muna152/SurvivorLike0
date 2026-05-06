using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile-type weapon: auto-fires towards the nearest enemy.
/// Supports multiple projectiles in a fan pattern.
/// </summary>
public class ProjectileWeapon : WeaponBase
{
    private bool _poolRegistered;

    public override void Initialize(WeaponData data, PlayerStats stats)
    {
        base.Initialize(data, stats);
        RegisterProjectilePool();
    }

    private void RegisterProjectilePool()
    {
        if (_poolRegistered || _data == null || _data.projectilePrefab == null) return;

        var prefab = _data.projectilePrefab;
        string key = prefab.name;

        PoolManager.Instance.Register<Projectile>(
            key,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<Projectile>();
            },
            proj => proj.ResetForReuse(),
            prewarmCount: 20,
            maxSize: 100
        );

        _poolRegistered = true;
    }

    protected override void Attack()
    {
        if (_data == null || _data.projectilePrefab == null) return;

        var ld = CurrentLevelData;
        if (ld == null) return;

        int count = ld.projectileCount + _playerStats.ProjectileBonus;

        // Spatial grid lookup — O(k) where k = enemies in nearby cells instead of O(n)
        const float queryRadius = 50f;
        EnemyBase nearest = SpatialGrid.QueryNearest(_playerPosition, queryRadius);

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