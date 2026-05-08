using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton drop manager: queues drop requests and spawns them
/// over multiple frames to avoid mass-instantiation spikes.
/// Nearby EXP/Gold drops are merged to reduce total object count.
/// </summary>
public class DropManager : Singleton<DropManager>
{
    [Header("Drop Table")]
    [SerializeField] private DropTableData _dropTable;

    [Header("Drop Prefabs")]
    [SerializeField] private GameObject _expGemPrefab;
    [SerializeField] private GameObject _goldCoinPrefab;
    [SerializeField] private GameObject _healthPrefab;
    [SerializeField] private GameObject _chestPrefab;
    [SerializeField] private GameObject _magnetPrefab;

    private struct PendingDrop
    {
        public DropBase.DropType Type;
        public Vector2 Position;
        public int Value;
        public float MagnetDuration;
        public float MagnetPickupBoost;
    }

    private readonly List<PendingDrop> _pending = new List<PendingDrop>(64);

    protected override void Awake()
    {
        base.Awake();
        RegisterDropPools();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _pending.Clear();
        RegisterDropPools();
    }

    private void RegisterDropPools()
    {
        RegisterDropPool(_expGemPrefab, "ExpGem");
        RegisterDropPool(_goldCoinPrefab, "Gold");
        RegisterDropPool(_healthPrefab, "Health");
        RegisterDropPool(_chestPrefab, "Chest");
        RegisterDropPool(_magnetPrefab, "Magnet");
    }

    private void RegisterDropPool(GameObject prefab, string key)
    {
        if (prefab == null) return;

        PoolManager.Instance.Register<DropBase>(
            key,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<DropBase>();
            },
            drop => drop.ResetForReuse(),
            prewarmCount: 30,
            maxSize: 200
        );
    }

    /// <summary>Queue drops at the given position. Merges nearby EXP/Gold requests.</summary>
    public void SpawnDrops(Vector2 position, int expValue, int goldValue, bool isElite = false, bool isBoss = false)
    {
        var t = _dropTable;
        if (t == null) return;

        Vector2 offset = Random.insideUnitCircle * 0.5f;
        Vector2 spawnPos = position + offset;

        // Determine EXP gem tier based on game time
        float minutes = GameManager.HasInstance ? GameManager.Instance.ElapsedTime / 60f : 0f;
        int expGemValue = t.expGemSmallValue;
        if (minutes >= t.expGemLargeThreshold || isBoss)
            expGemValue = t.expGemLargeValue;
        else if (minutes >= t.expGemMediumThreshold || isElite)
            expGemValue = t.expGemMediumValue;

        int scaledExpValue = expValue * expGemValue;

        // EXP gem — merge with nearby pending gem
        if (_expGemPrefab != null && Random.value <= t.expGemChance)
        {
            EnqueueOrMerge(DropBase.DropType.ExpGem, spawnPos, scaledExpValue);
        }

        // Health — rare, no merging needed
        if (_healthPrefab != null && Random.value <= t.healthChance)
        {
            _pending.Add(new PendingDrop
            {
                Type = DropBase.DropType.Health,
                Position = spawnPos + Random.insideUnitCircle * 0.3f,
                Value = t.healthValue
            });
        }

        // Chest — drops from elite/boss only
        if (_chestPrefab != null && (isElite || isBoss) && Random.value <= t.chestChance)
        {
            _pending.Add(new PendingDrop
            {
                Type = DropBase.DropType.Chest,
                Position = spawnPos + Random.insideUnitCircle * 0.4f,
                Value = 1
            });
        }

        // Magnet — rare drop
        if (_magnetPrefab != null && Random.value <= t.magnetChance)
        {
            _pending.Add(new PendingDrop
            {
                Type = DropBase.DropType.Magnet,
                Position = spawnPos + Random.insideUnitCircle * 0.4f,
                Value = 1,
                MagnetDuration = t.magnetDuration,
                MagnetPickupBoost = t.magnetPickupBoost
            });
        }

        // Gold coin — merge with nearby pending gold
        if (_goldCoinPrefab != null && goldValue > 0)
        {
            EnqueueOrMerge(DropBase.DropType.Gold, spawnPos + Random.insideUnitCircle * 0.3f, goldValue);
        }
    }

    private void EnqueueOrMerge(DropBase.DropType type, Vector2 pos, int value)
    {
        float mergeSq = _dropTable != null ? _dropTable.mergeRadius * _dropTable.mergeRadius : 6.25f;

        for (int i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Type != type) continue;

            float distSq = (pos - _pending[i].Position).sqrMagnitude;
            if (distSq <= mergeSq)
            {
                // Merge: accumulate value, keep the earlier position
                _pending[i] = new PendingDrop
                {
                    Type = type,
                    Position = _pending[i].Position,
                    Value = _pending[i].Value + value
                };
                return;
            }
        }

        // No merge candidate — add new request
        _pending.Add(new PendingDrop
        {
            Type = type,
            Position = pos,
            Value = value
        });
    }

    private void Update()
    {
        if (_pending.Count == 0) return;

        int maxSpawns = _dropTable != null ? _dropTable.maxSpawnsPerFrame : 6;
        int spawned = 0;
        int startIdx = 0;

        while (startIdx < _pending.Count && spawned < maxSpawns)
        {
            PendingDrop drop = _pending[startIdx];

            string poolKey = drop.Type.ToString();
            var dropObj = PoolManager.Instance.Get<DropBase>(poolKey);
            if (dropObj != null)
            {
                dropObj.transform.position = drop.Position;
                dropObj.SetValue(drop.Value);

                // Apply magnet config from drop table
                if (drop.Type == DropBase.DropType.Magnet)
                {
                    dropObj.SetMagnetConfig(drop.MagnetDuration, drop.MagnetPickupBoost);
                }
            }

            // Remove by swapping with last (O(1) removal)
            int lastIdx = _pending.Count - 1;
            if (startIdx != lastIdx)
                _pending[startIdx] = _pending[lastIdx];
            _pending.RemoveAt(lastIdx);

            // Don't increment startIdx — we swapped a new element into startIdx
            spawned++;
        }
    }
}