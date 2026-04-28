using UnityEngine;

/// <summary>
/// Singleton drop manager: spawns drops when enemies die.
/// </summary>
public class DropManager : Singleton<DropManager>
{
    [Header("Drop Prefabs")]
    [SerializeField] private GameObject _expGemPrefab;
    [SerializeField] private GameObject _goldCoinPrefab;
    [SerializeField] private GameObject _healthPrefab;

    [Header("Drop Rates")]
    [Range(0f, 1f)] [SerializeField] private float _expGemChance = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float _healthChance = 0.05f;

    protected override void Awake()
    {
        base.Awake();
        RegisterDropPools();
    }

    private void RegisterDropPools()
    {
        RegisterDropPool(_expGemPrefab, "ExpGem");
        RegisterDropPool(_goldCoinPrefab, "Gold");
        RegisterDropPool(_healthPrefab, "Health");
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

    /// <summary>Spawn drops at the given position.</summary>
    public void SpawnDrops(Vector2 position, int expValue, int goldValue)
    {
        // Small random offset
        Vector2 offset = Random.insideUnitCircle * 0.5f;
        Vector2 spawnPos = position + offset;

        // EXP gem
        if (_expGemPrefab != null && Random.value <= _expGemChance)
        {
            var gem = SpawnDrop(_expGemPrefab, spawnPos, "ExpGem");
            if (gem != null) gem.SetValue(expValue);
        }

        // Health
        if (_healthPrefab != null && Random.value <= _healthChance)
        {
            var health = SpawnDrop(_healthPrefab, spawnPos + Random.insideUnitCircle * 0.3f, "Health");
            if (health != null) health.SetValue(30);
        }

        // Gold coin (always)
        if (_goldCoinPrefab != null && goldValue > 0)
        {
            var coin = SpawnDrop(_goldCoinPrefab, spawnPos + Random.insideUnitCircle * 0.3f, "Gold");
            if (coin != null) coin.SetValue(goldValue);
        }
    }

    private DropBase SpawnDrop(GameObject prefab, Vector2 pos, string poolKey)
    {
        var dropObj = PoolManager.Instance.Get<DropBase>(poolKey);
        if (dropObj == null) return null;

        dropObj.transform.position = pos;
        return dropObj;
    }
}