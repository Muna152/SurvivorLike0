using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns enemy waves around the player at configurable intervals.
/// Also spawns bosses at timed intervals (10/20/30 minutes).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float _baseSpawnInterval = 0.8f;  // Changed from 1.5f
    [SerializeField] private float _bossSpawnDistance = 18f;

    private float _spawnTimer;
    private float _eliteWaveTimer;
    private PlayerController _player;
    private bool _poolsRegistered;
    private readonly List<WeightedRandom.WeightedItem<EnemyData>> _availableBuffer
        = new List<WeightedRandom.WeightedItem<EnemyData>>();

    /// <summary>True when enemy count has reached the cap and spawning is paused.</summary>
    public bool IsSpawnCapped { get; private set; }

    // Cached from EnemyDatabase
    private EnemyData[] _enemyPool;
    private EnemyData _skeletonKingData;
    private EnemyData _darkLordData;
    private EnemyData _deathBossData;

    // Boss tracking — each boss spawns only once per run
    private bool _skeletonKingSpawned;
    private bool _darkLordSpawned;
    private bool _deathBossSpawned;

    private void Awake()
    {
        // Ensure EnemyManager exists so the centralized tick loop runs
        _ = EnemyManager.Instance;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Defer pool registration — other systems (VFXManager, DropManager) may clear
        // pools in their own sceneLoaded callbacks, which can run after ours.
        // Setting the flag to false lets Update re-register lazily when safe.
        _poolsRegistered = false;
    }

    private void Start()
    {
        LoadEnemyData();

        // Ensure pools are registered (safety net for domain reload issues)
        if (!_poolsRegistered)
            RegisterEnemyPools();

        _player = FindObjectOfType<PlayerController>();
        EnemyBase.SetPlayerReference(_player);
        DropBase.SetPlayerReference(_player);
    }

    /// <summary>Load enemy and boss data from EnemyDatabase singleton.</summary>
    private void LoadEnemyData()
    {
        var db = EnemyDatabase.Instance;
        if (db != null)
        {
            _enemyPool = db.enemies;

            if (db.bosses != null)
            {
                _skeletonKingData = db.GetBossById("skeleton_king");
                _darkLordData = db.GetBossById("dark_lord");
                _deathBossData = db.GetBossById("death");
            }
        }

        if (_enemyPool == null || _enemyPool.Length == 0)
            Debug.LogError("[EnemySpawner] EnemyDatabase has no enemies assigned!");
    }

    private void Update()
    {
        if (_player == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Lazy pool registration — ensures pools exist before any spawning.
        // Handles delayed init and scene-load re-registration after other systems have cleared pools.
        if (!_poolsRegistered)
            RegisterEnemyPools();

        if (_enemyPool == null || _enemyPool.Length == 0)
            return;

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
        float eliteInterval = GameBalanceConfig.Instance != null
            ? GameBalanceConfig.Instance.eliteWaveInterval
            : 360f;
        _eliteWaveTimer -= Time.deltaTime;
        if (_eliteWaveTimer <= 0f)
        {
            SpawnEliteWave();
            _eliteWaveTimer = eliteInterval;
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
            LoadEnemyData();
        if (_enemyPool == null)
        {
            Debug.LogWarning("[EnemySpawner] _enemyPool is still null after LoadEnemyData — skipping pool registration.");
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
        var cfg = GameBalanceConfig.Instance;
        int maxEnemies = cfg != null ? cfg.maxEnemiesOnScreen : 500;
        float minDist = cfg != null ? cfg.minSpawnDistance : 15f;
        float maxDist = cfg != null ? cfg.maxSpawnDistance : 25f;

        if (EnemyBase.ActiveEnemyCount >= maxEnemies)
        {
            IsSpawnCapped = true;
            return;
        }
        IsSpawnCapped = false;

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

        // Batch size scales with time (use config if available)
        int baseBatch = cfg != null ? cfg.baseBatchSize : 3;
        float batchGrowth = cfg != null ? cfg.batchGrowthRate : 0.8f;
        int maxBatch = cfg != null ? cfg.maxBatchSize : 15;
        int batchSize = Mathf.Min(maxBatch, Mathf.FloorToInt(baseBatch + batchGrowth * minutes));
        Vector2 playerPos = (Vector2)_player.transform.position;

        for (int i = 0; i < batchSize; i++)
        {
            if (EnemyBase.ActiveEnemyCount >= maxEnemies) break;

            var data = WeightedRandom.Select(_availableBuffer);
            Vector2 pos = GetSpawnPosition(playerPos, minDist, maxDist);

            var enemyObj = PoolManager.Instance.Get<EnemyBase>(data.enemyName);
            if (enemyObj != null)
            {
                enemyObj.transform.position = pos;
                enemyObj.Initialize(data);
            }
        }
    }

    private Vector2 GetSpawnPosition(Vector2 center, float minDist = 15f, float maxDist = 25f)
    {
        var cfg = GameBalanceConfig.Instance;
        float min = minDist > 0 ? minDist : (cfg != null ? cfg.minSpawnDistance : 15f);
        float max = maxDist > 0 ? maxDist : (cfg != null ? cfg.maxSpawnDistance : 25f);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(min, max);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    private void SpawnEliteWave()
    {
        var cfg = GameBalanceConfig.Instance;
        int maxEnemies = cfg != null ? cfg.maxEnemiesOnScreen : 500;
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

        // Elite count from config, scaling with wave number
        int minCount = cfg != null ? cfg.eliteWaveMinCount : 2;
        int maxCount = cfg != null ? cfg.eliteWaveMaxCount : 4;
        int bonusPerWave = cfg != null ? cfg.eliteWaveBonusPerWave : 3;
        int cap = cfg != null ? cfg.eliteWaveCap : 20;
        int waveBonus = DifficultyManager.HasInstance ? DifficultyManager.Instance.EliteWaveCount * bonusPerWave : 0;
        int eliteCount = Mathf.Min(cap, Random.Range(minCount, maxCount + 1) + waveBonus);
        Vector2 playerPos = (Vector2)_player.transform.position;

        for (int i = 0; i < eliteCount; i++)
        {
            if (EnemyBase.ActiveEnemyCount >= maxEnemies) break;

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