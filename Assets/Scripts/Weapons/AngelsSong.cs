using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Angel's Song weapon: Evolved form of Holy Light.
/// Stronger slow, low damage, steady ~1 HP/sec healing.
/// A defensive utility weapon that trades raw power for survivability.
/// </summary>
public class AngelsSong : AreaWeapon
{
    [Header("Angel's Song Settings")]
    [Tooltip("Speed multiplier applied to slowed enemies (0.6 = 40% slow)")]
    [SerializeField] protected float slowFactor = 0.6f;
    [Tooltip("Duration of slow effect on each tick (seconds)")]
    [SerializeField] protected float slowDuration = 0.8f;
    [Tooltip("Fixed healing per tick (with 0.5s interval = 1 HP/sec)")]
    [SerializeField] protected float healPerTick = 0.5f;

    // Reusable list for slow queries
    private static readonly List<EnemyBase> _slowResults = new List<EnemyBase>(32);

    protected override bool PlayAttackEffects => false;

    private void Awake()
    {
        _followsPlayer = true;
    }

    protected override void ApplyAreaEffect()
    {
        Vector2 center = _currentArea != null ? (Vector2)_currentArea.transform.position : _areaOrigin;
        float radius = GetDamageRadius();

        // 1. Slow enemies in radius
        SpatialGrid.QueryInRadius(center, radius, _slowResults);
        for (int i = 0; i < _slowResults.Count; i++)
        {
            var enemy = _slowResults[i];
            if (enemy == null) continue;

            // Apply slow
            enemy.ApplySlow(slowFactor, slowDuration);

            // Apply low damage
            var ld = CurrentLevelData;
            if (ld != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(ld.damage * _playerStats.DamageMultiplier));
            }
        }

        // 2. Heal player with fixed amount (~1 HP/sec at default tick interval)
        if (_playerStats != null)
        {
            _playerStats.Heal(healPerTick);
        }
    }
}
