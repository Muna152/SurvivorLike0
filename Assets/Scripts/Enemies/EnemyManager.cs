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

    protected override void OnDestroy()
    {
        GameEvents.OnEnemyDied -= OnEnemyDiedHandler;
        base.OnDestroy();
    }
}
