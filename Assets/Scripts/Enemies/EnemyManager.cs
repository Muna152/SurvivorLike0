using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton managing all active enemies in the scene.
/// Subscribes to OnEnemyDied to remove enemies from tracking.
/// </summary>
public class EnemyManager : Singleton<EnemyManager>
{
    private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();
    private PlayerStats _playerStats;

    public int ActiveCount => _activeEnemies.Count;

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
        _activeEnemies.Add(enemyObj);
        GameEvents.InvokeEnemySpawned(enemyObj);
        return enemyObj;
    }

    private void OnEnemyDiedHandler(EnemyBase enemy)
    {
        _activeEnemies.Remove(enemy);
        if (_playerStats != null) _playerStats.AddKill();
    }

    protected override void OnDestroy()
    {
        GameEvents.OnEnemyDied -= OnEnemyDiedHandler;
        base.OnDestroy();
    }
}
