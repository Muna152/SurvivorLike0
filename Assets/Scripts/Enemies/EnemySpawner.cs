using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemy waves around the player at configurable intervals.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyData[] _enemyPool;
    [SerializeField] private float _baseSpawnInterval = 1.5f;
    [SerializeField] private float _minSpawnDistance = 15f;
    [SerializeField] private float _maxSpawnDistance = 25f;
    [SerializeField] private int _maxEnemiesOnScreen = 500;

    private float _spawnTimer;
    private PlayerController _player;

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (_player == null) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnWave();
            _spawnTimer = _baseSpawnInterval;
        }
    }

    private void SpawnWave()
    {
        int active = FindObjectsOfType<EnemyBase>().Length;
        if (active >= _maxEnemiesOnScreen) return;

        float minutes = GameManager.Instance.ElapsedTime / 60f;

        // Filter available enemies by minSpawnTime
        var available = new List<WeightedRandom.WeightedItem<EnemyData>>();
        foreach (var ed in _enemyPool)
        {
            if (minutes >= ed.minSpawnTime / 60f)
            {
                available.Add(new WeightedRandom.WeightedItem<EnemyData>(ed, ed.spawnWeight));
            }
        }
        if (available.Count == 0) return;

        // Batch size scales with time
        int batchSize = Mathf.Min(15, Mathf.FloorToInt(3 + 0.8f * minutes));
        Vector2 playerPos = (Vector2)_player.transform.position;

        for (int i = 0; i < batchSize; i++)
        {
            if (FindObjectsOfType<EnemyBase>().Length >= _maxEnemiesOnScreen) break;

            var data = WeightedRandom.Select(available);
            Vector2 pos = GetSpawnPosition(playerPos);

            var enemyObj = PoolManager.Instance.Get<EnemyBase>(data.enemyName);
            if (enemyObj != null)
            {
                enemyObj.transform.position = pos;
                enemyObj.Initialize(data);
            }
        }
    }

    private Vector2 GetSpawnPosition(Vector2 center)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(_minSpawnDistance, _maxSpawnDistance);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }
}