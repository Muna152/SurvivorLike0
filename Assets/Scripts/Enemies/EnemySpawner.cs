using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemy waves around the player at configurable intervals.
/// Also spawns bosses at timed intervals (10/20/30 minutes).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyData[] _enemyPool;
    [SerializeField] private float _baseSpawnInterval = 1.5f;
    [SerializeField] private float _minSpawnDistance = 15f;
    [SerializeField] private float _maxSpawnDistance = 25f;
    [SerializeField] private int _maxEnemiesOnScreen = 500;
    [SerializeField] private float _eliteWaveInterval = 300f; // 5 minutes in seconds

    [Header("Boss Spawning")]
    [SerializeField] private EnemyData _skeletonKingData;
    [SerializeField] private EnemyData _darkLordData;
    [SerializeField] private EnemyData _deathBossData;
    [SerializeField] private float _bossSpawnDistance = 18f;

    private float _spawnTimer;
    private float _eliteWaveTimer;
    private PlayerController _player;
    private bool _poolsRegistered;
    private readonly List<WeightedRandom.WeightedItem<EnemyData>> _availableBuffer
        = new List<WeightedRandom.WeightedItem<EnemyData>>();

    // Boss tracking — each boss spawns only once per run
    private bool _skeletonKingSpawned;
    private bool _darkLordSpawned;
    private bool _deathBossSpawned;

    private void Awake()
    {
        RegisterEnemyPools();
    }

    private void Start()
    {
        // Ensure pools are registered (safety net for domain reload issues)
        if (!_poolsRegistered)
            RegisterEnemyPools();

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
            float intervalScale = DifficultyManager.HasInstance
                ? DifficultyManager.Instance.SpawnIntervalMultiplier
                : 1f;
            _spawnTimer = _baseSpawnInterval * intervalScale;
        }

        // Elite wave timer
        _eliteWaveTimer -= Time.deltaTime;
        if (_eliteWaveTimer <= 0f)
        {
            SpawnEliteWave();
            _eliteWaveTimer = _eliteWaveInterval;
        }

        // Boss spawn checks
        CheckBossSpawns();
    }

    private void CheckBossSpawns()
    {
        float minutes = GameManager.Instance != null ? GameManager.Instance.ElapsedTime / 60f : 0f;

        // 10 min — Skeleton King
        if (!_skeletonKingSpawned && minutes >= 10f && _skeletonKingData != null)
        {
            SpawnBoss(_skeletonKingData);
            _skeletonKingSpawned = true;
        }

        // 20 min — Dark Lord
        if (!_darkLordSpawned && minutes >= 20f && _darkLordData != null)
        {
            SpawnBoss(_darkLordData);
            _darkLordSpawned = true;
        }

        // 30 min — Death
        if (!_deathBossSpawned && minutes >= 30f && _deathBossData != null)
        {
            SpawnBoss(_deathBossData);
            _deathBossSpawned = true;
        }
    }

    /// <summary>Spawn a boss at a fixed distance from the player. Bosses are instantiated (not pooled).</summary>
    private void SpawnBoss(EnemyData bossData)
    {
        if (bossData.prefab == null || _player == null) return;

        Vector2 playerPos = (Vector2)_player.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 spawnPos = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _bossSpawnDistance;

        GameObject bossObj = Instantiate(bossData.prefab, spawnPos, Quaternion.identity);
        var boss = bossObj.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.Initialize(bossData);
            Debug.Log($"[EnemySpawner] Boss spawned: {bossData.enemyName} at {spawnPos}");
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Boss prefab missing BossEnemy component: {bossData.enemyName}");
        }
    }

    /// <summary>Reset boss spawn flags for a new game run.</summary>
    public void ResetBossFlags()
    {
        _skeletonKingSpawned = false;
        _darkLordSpawned = false;
        _deathBossSpawned = false;
    }

    private void RegisterEnemyPools()
    {
        if (_poolsRegistered) return;
        if (_enemyPool == null)
        {
            Debug.LogError("[EnemySpawner] _enemyPool is null! Cannot register pools.");
            return;
        }

        Debug.Log($"[EnemySpawner] Registering {_enemyPool.Length} enemy pools...");

        foreach (var ed in _enemyPool)
        {
            if (ed == null || ed.prefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Skipping null entry in _enemyPool.");
                continue;
            }

            var prefab = ed.prefab;
            string key = ed.enemyName;

            // Use GetOrRegister pattern: if pool already exists (from a previous session), skip
            if (PoolManager.HasInstance && PoolManager.Instance.HasPool(key))
                continue;

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

            Debug.Log($"[EnemySpawner] Registered pool: {key}");
        }

        _poolsRegistered = true;
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
                // Higher minSpawnTime enemies get a weight boost in elite waves
                float weightBoost = 1f + ed.minSpawnTime / 120f;
                _availableBuffer.Add(new WeightedRandom.WeightedItem<EnemyData>(ed, ed.spawnWeight * weightBoost));
            }
        }
        if (_availableBuffer.Count == 0) return;

        // Elite count scales with wave number: base 5-10 + 2 per previous wave
        int waveBonus = DifficultyManager.HasInstance ? DifficultyManager.Instance.EliteWaveCount * 2 : 0;
        int eliteCount = Mathf.Min(20, Random.Range(5, 11) + waveBonus);
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
                enemyObj.SetElite();
            }
        }

        // Track elite wave count for difficulty progression
        if (DifficultyManager.HasInstance)
            DifficultyManager.Instance.OnEliteWaveSpawned();
    }
}