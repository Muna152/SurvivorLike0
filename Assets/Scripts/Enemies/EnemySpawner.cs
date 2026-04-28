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
    [SerializeField] private float _eliteWaveInterval = 300f; // 5 minutes in seconds

    private float _spawnTimer;
    private float _eliteWaveTimer;
    private PlayerController _player;
    private readonly List<WeightedRandom.WeightedItem<EnemyData>> _availableBuffer
        = new List<WeightedRandom.WeightedItem<EnemyData>>();

    private void Awake()
    {
        RegisterEnemyPools();
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        EnemyBase.SetPlayerReference(_player);
        DropBase.SetPlayerReference(_player);
    }

    private void Update()
    {
        if (_player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Regular spawn
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnWave();
            _spawnTimer = _baseSpawnInterval;
        }

        // Elite wave timer
        _eliteWaveTimer -= Time.deltaTime;
        if (_eliteWaveTimer <= 0f)
        {
            SpawnEliteWave();
            _eliteWaveTimer = _eliteWaveInterval;
        }
    }

    private void RegisterEnemyPools()
    {
        if (_enemyPool == null) return;

        foreach (var ed in _enemyPool)
        {
            if (ed == null || ed.prefab == null) continue;

            var prefab = ed.prefab;
            string key = ed.enemyName;

            PoolManager.Instance.Register<EnemyBase>(
                key,
                () =>
                {
                    var obj = Instantiate(prefab);
                    obj.SetActive(false);
                    return obj.GetComponent<EnemyBase>();
                },
                enemy => enemy.ResetForReuse(),
                prewarmCount: 30,
                maxSize: 200
            );
        }
    }

    private void SpawnWave()
    {
        if (EnemyBase.ActiveEnemyCount >= _maxEnemiesOnScreen) return;

        float minutes = GameManager.Instance.ElapsedTime / 60f;

        // Reuse cached list to avoid GC allocation
        _availableBuffer.Clear();
        foreach (var ed in _enemyPool)
        {
            if (minutes >= ed.minSpawnTime / 60f)
            {
                _availableBuffer.Add(new WeightedRandom.WeightedItem<EnemyData>(ed, ed.spawnWeight));
            }
        }
        if (_availableBuffer.Count == 0) return;

        // Batch size scales with time
        int batchSize = Mathf.Min(15, Mathf.FloorToInt(3 + 0.8f * minutes));
        Vector2 playerPos = (Vector2)_player.transform.position;

        for (int i = 0; i < batchSize; i++)
        {
            if (EnemyBase.ActiveEnemyCount >= _maxEnemiesOnScreen) break;

            var data = WeightedRandom.Select(_availableBuffer);
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

    private void SpawnEliteWave()
    {
        float minutes = GameManager.Instance.ElapsedTime / 60f;

        _availableBuffer.Clear();
        foreach (var ed in _enemyPool)
        {
            if (minutes >= ed.minSpawnTime / 60f)
            {
                _availableBuffer.Add(new WeightedRandom.WeightedItem<EnemyData>(ed, ed.spawnWeight));
            }
        }
        if (_availableBuffer.Count == 0) return;

        // Spawn 5-10 elite enemies
        int eliteCount = Random.Range(5, 11);
        Vector2 playerPos = (Vector2)_player.transform.position;

        for (int i = 0; i < eliteCount; i++)
        {
            if (EnemyBase.ActiveEnemyCount >= _maxEnemiesOnScreen) break;

            var data = WeightedRandom.Select(_availableBuffer);
            Vector2 pos = GetSpawnPosition(playerPos);

            var enemyObj = PoolManager.Instance.Get<EnemyBase>(data.enemyName);
            if (enemyObj != null)
            {
                enemyObj.transform.position = pos;
                enemyObj.Initialize(data);

                // Make it elite
                if (enemyObj is EliteEnemy elite)
                {
                    elite.Initialize(data);
                }
                else
                {
                    enemyObj.SetElite();
                }
            }
        }
    }
}