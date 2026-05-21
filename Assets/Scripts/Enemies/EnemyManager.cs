using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton managing all active enemies in the scene.
/// Drives centralized update via IEnemyTick. Uses EnemyBase.ActiveEnemies
/// (static HashSet) as the single source of truth — all enemies register
/// there in Initialize(), regardless of spawn path.
/// </summary>
public class EnemyManager : Singleton<EnemyManager>
{
    private PlayerStats _playerStats;

    // Pre-allocated list for separation neighbor queries
    private readonly List<EnemyBase> _separationNeighbors = new List<EnemyBase>(16);

    // Separation constants
    private const float OverlapAllowance = 0.23f; // Allow ~77% overlap (gap ≈ 1/3 of original)
    private const float PushFactor = 0.5f;       // Smoothing per frame

    public int ActiveCount => EnemyBase.ActiveEnemyCount;

    protected override void Awake()
    {
        base.Awake();
        GameEvents.OnEnemyDied += OnEnemyDiedHandler;
    }

    private void Start()
    {
        _playerStats = FindObjectOfType<PlayerStats>();
    }

    public EnemyBase SpawnEnemy(EnemyData data, Vector2 position)
    {
        var enemyObj = PoolManager.Instance.Get<EnemyBase>(data.enemyName);
        if (enemyObj == null) return null;

        enemyObj.transform.position = position;
        enemyObj.Initialize(data);
        GameEvents.InvokeEnemySpawned(enemyObj);
        return enemyObj;
    }

    private void OnEnemyDiedHandler(EnemyBase enemy)
    {
        if (_playerStats != null)
        {
            _playerStats.AddKill();
            if (enemy.IsElite)
                _playerStats.AddEliteKill();
        }
    }

    private void FixedUpdate()
    {
        SpatialGrid.UpdateAll();

        float dt = Time.fixedDeltaTime;
        EnemyBase.IsIterating = true;
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null)
                enemy.OnFixedTick(dt);
        }
        EnemyBase.IsIterating = false;
        EnemyBase.FlushPendingRemoves();

        // Separation pass: push enemies apart to prevent overlap
        SeparationPass();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        EnemyBase.IsIterating = true;
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null)
                enemy.OnUpdateTick(dt);
        }
        EnemyBase.IsIterating = false;
        EnemyBase.FlushPendingRemoves();
    }

    /// <summary>
    /// Soft separation pass: push enemies apart when they overlap more than 30%.
    /// Uses SpatialGrid for efficient neighbor queries.
    /// </summary>
    private void SeparationPass()
    {
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == null) continue;

            float r1 = enemy.SeparationRadius;
            if (r1 <= 0f) continue;

            Vector2 enemyPos = (Vector2)enemy.transform.position;
            float queryRadius = r1 * 2.2f; // Slightly beyond max separation range

            SpatialGrid.QueryInRadius(enemyPos, queryRadius, _separationNeighbors);

            Vector2 push = Vector2.zero;
            for (int i = 0; i < _separationNeighbors.Count; i++)
            {
                var other = _separationNeighbors[i];
                if (other == enemy) continue;

                float r2 = other.SeparationRadius;
                if (r2 <= 0f) continue;

                float minDist = (r1 + r2) * OverlapAllowance;
                Vector2 diff = enemyPos - (Vector2)other.transform.position;
                float distSq = diff.sqrMagnitude;

                if (distSq < minDist * minDist)
                {
                    float dist = Mathf.Sqrt(distSq);
                    if (dist > 0.001f)
                    {
                        float overlap = minDist - dist;
                        push += (diff / dist) * overlap;
                    }
                    else
                    {
                        // Coincident enemies — deterministic push direction
                        float angle = ((enemy.GetInstanceID() * 2.345f) % 6.283185f);
                        push += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * minDist;
                    }
                }
            }

            if (push.sqrMagnitude > 0.0001f)
            {
                enemy.ApplySeparationPush(push * PushFactor);
            }
        }
    }

    protected override void OnDestroy()
    {
        GameEvents.OnEnemyDied -= OnEnemyDiedHandler;
        base.OnDestroy();
    }
}
