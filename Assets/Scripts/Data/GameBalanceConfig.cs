using UnityEngine;

/// <summary>
/// Centralized game balance configuration (ScriptableObject).
/// Replaces hardcoded values scattered across gameplay scripts with tunable data.
/// Create via Assets/Create/Data/GameBalanceConfig.
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Data/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    private static GameBalanceConfig _instance;
    /// <summary>Singleton accessor — loads from Resources/GameBalanceConfig.</summary>
    public static GameBalanceConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
            return _instance;
        }
    }

    [Header("Difficulty Scaling")]
    [Tooltip("Enemy HP growth rate per minute (+8% = 0.08)")]
    public float hpGrowthRate = 0.08f;
    [Tooltip("Spawn interval acceleration rate per minute")]
    public float spawnAccelRate = 0.12f;
    [Tooltip("Enemy damage growth rate per minute (+3% = 0.03)")]
    public float damageGrowthRate = 0.03f;
    [Tooltip("Enemy move speed growth rate per minute (+1.5% = 0.015)")]
    public float speedGrowthRate = 0.015f;

    [Header("Elite Enemy")]
    [Tooltip("HP multiplier applied when an enemy becomes elite")]
    public float eliteHpMultiplier = 5f;
    [Tooltip("Damage multiplier applied when an enemy becomes elite")]
    public float eliteDamageMultiplier = 2f;
    [Tooltip("Scale multiplier applied to elite enemy visuals")]
    public float eliteScaleMultiplier = 1.05f;
    [Tooltip("Move speed multiplier applied to elite enemies")]
    public float eliteSpeedMultiplier = 1.2f;
    [Tooltip("EXP drop multiplier for elite enemies")]
    public int eliteExpMultiplier = 10;
    [Tooltip("Gold drop multiplier for elite enemies")]
    public int eliteGoldMultiplier = 5;

    [Header("Boss")]
    [Tooltip("Visual scale multiplier for all bosses")]
    public float bossScaleMultiplier = 2f;
    [Tooltip("Base attack interval (seconds) for bosses")]
    public float bossAttackInterval = 3f;
    [Tooltip("Attack interval multiplier per phase (e.g. 0.8 = 20% faster each phase)")]
    public float bossAttackIntervalPhaseMultiplier = 0.8f;
    [Tooltip("EXP multiplier for boss kills (applied to EnemyData.expValue)")]
    public int bossExpMultiplier = 50;
    [Tooltip("Gold multiplier for boss kills (applied to EnemyData.goldValue)")]
    public int bossGoldMultiplier = 20;
    [Tooltip("Invulnerability duration (seconds) during boss phase transitions")]
    public float bossPhaseTransitionDuration = 0.5f;

    [Header("Spawner")]
    [Tooltip("Minimum spawn distance from player")]
    public float minSpawnDistance = 15f;
    [Tooltip("Maximum spawn distance from player")]
    public float maxSpawnDistance = 25f;
    [Tooltip("Global enemy cap on screen")]
    public int maxEnemiesOnScreen = 500;
    [Tooltip("Elite wave interval (seconds)")]
    public float eliteWaveInterval = 300f;
    [Tooltip("Boss spawn distance from player")]
    public float bossSpawnDistance = 18f;
    [Tooltip("Base batch size formula: floor(baseBatch + batchGrowthRate * minutes)")]
    public int baseBatchSize = 3;
    [Tooltip("Batch size growth rate per minute")]
    public float batchGrowthRate = 0.8f;
    [Tooltip("Maximum batch size per wave")]
    public int maxBatchSize = 15;
    [Tooltip("Minimum elite wave count")]
    public int eliteWaveMinCount = 5;
    [Tooltip("Maximum elite wave count (before cap)")]
    public int eliteWaveMaxCount = 10;
    [Tooltip("Elite wave count cap")]
    public int eliteWaveCap = 20;
    [Tooltip("Elite wave bonus per previous wave")]
    public int eliteWaveBonusPerWave = 2;

    [Header("Area Weapon")]
    [Tooltip("Tick interval (seconds) for AreaWeapon damage/healing pulses")]
    public float areaWeaponTickInterval = 0.5f;
    [Tooltip("Buffer radius added to visual size for damage detection")]
    public float areaWeaponDamageRadiusBuffer = 0.15f;

    [Header("Orbital Weapon")]
    [Tooltip("Rotation speed (degrees/second) for orbital weapons")]
    public float orbitalRotationSpeed = 120f;
    [Tooltip("Hit cooldown (seconds) for orbital objects hitting same enemy")]
    public float orbitalHitCooldown = 0.15f;

    [Header("Projectile")]
    [Tooltip("Maximum travel distance before auto-return to pool")]
    public float projectileMaxRange = 30f;
    [Tooltip("Search radius for finding nearest enemy target")]
    public float projectileSearchRadius = 50f;

    [Header("Player")]
    [Tooltip("HP percentage restored on extra life revive (0-1)")]
    public float extraLifeReviveHpPercent = 0.5f;
    [Tooltip("HP regeneration tick interval (seconds)")]
    public float regenTickInterval = 1f;

    [Header("Drop Rates")]
    [Tooltip("Chance to drop EXP gem on enemy kill")]
    [Range(0f, 1f)] public float expGemChance = 0.75f;
    [Tooltip("Chance to drop health item on enemy kill")]
    [Range(0f, 1f)] public float healthDropChance = 0.02f;
    [Tooltip("Chance to drop chest on elite/boss kill")]
    [Range(0f, 1f)] public float chestDropChance = 0.015f;
    [Tooltip("Chance to drop magnet item on enemy kill")]
    [Range(0f, 1f)] public float magnetDropChance = 0.01f;

    [Header("EXP Gem Tiers")]
    public int expGemSmallValue = 1;
    public int expGemMediumValue = 5;
    public int expGemLargeValue = 20;
    [Tooltip("Game minutes to start dropping medium gems")] public float expGemMediumThreshold = 8f;
    [Tooltip("Game minutes to start dropping large gems")] public float expGemLargeThreshold = 18f;

    [Header("Health & Magnet Drops")]
    public int healthRestoreAmount = 30;
    public float magnetDropDuration = 10f;
    public float magnetDropPickupBoost = 5f;

    [Header("Drop Performance")]
    public int maxDropsPerFrame = 6;
    public float dropMergeRadius = 2.5f;

    [Header("Drop Physics")]
    [Tooltip("Speed at which drops move toward player when attracted")]
    public float dropAttractSpeed = 15f;
    [Tooltip("Collection radius for auto-pickup")]
    public float dropCollectRadius = 0.5f;
    [Tooltip("Vacuum mode speed multiplier")]
    public float dropVacuumSpeedMultiplier = 2f;
    [Tooltip("Vacuum ease-in acceleration rate")]
    public float dropVacuumEaseRate = 2.5f;
    [Tooltip("Time after landing before vacuum activation (seconds)")]
    public float dropVacuumDelay = 2.5f;
    [Tooltip("Distance at which vacuum attraction begins")]
    public float dropVacuumRange = 8f;
}
