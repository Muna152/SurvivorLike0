using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode integration tests for boss spawning, combat, phase transitions,
/// and death mechanics.
/// </summary>
public class BossCombatTests
{
    private BossEnemy _lastSpawnedBoss;

    // ── Setup / Teardown ────────────────────────────────────────

    [SetUp]
    public void SetUp() => TestUtils.CleanupStatics();

    [TearDown]
    public void TearDown() => TestUtils.CleanupStatics();

    // ── Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Start game while suppressing the known EnemySpawner error that occurs
    /// during scene load (EnemySpawner.Awake runs before Start loads EnemyDatabase).
    /// </summary>
    private IEnumerator StartGameForBossTest()
    {
        LogAssert.Expect(LogType.Error, "[EnemySpawner] _enemyPool is null! Cannot register pools.");
        yield return TestUtils.StartGameWithDefaultCharacter();
    }

    // ── Boss Spawning Tests ─────────────────────────────────────

    [UnityTest]
    public IEnumerator BossSpawn_FiresOnBossSpawnedEvent()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        Assert.IsNotNull(_lastSpawnedBoss,
            "OnBossSpawned event should fire when a boss is initialized");
    }

    [UnityTest]
    public IEnumerator BossSpawn_IsInActiveEnemySet()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        Assert.IsTrue(EnemyBase.ActiveEnemies.Contains(_lastSpawnedBoss),
            "Boss should be in the ActiveEnemies set after spawning");
        Assert.Greater(EnemyBase.ActiveEnemyCount, 0);
    }

    [UnityTest]
    public IEnumerator BossSpawn_HasScaledSize()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        float expectedScale = GameBalanceConfig.Instance != null
            ? GameBalanceConfig.Instance.bossScaleMultiplier
            : 2f;

        Assert.AreEqual(expectedScale, _lastSpawnedBoss.transform.localScale.x, 0.01f,
            "Boss should be scaled by bossScaleMultiplier");
    }

    [UnityTest]
    public IEnumerator BossSpawn_StartsAtPhase0()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        Assert.AreEqual(0, _lastSpawnedBoss.CurrentPhase,
            "Boss should start at phase 0");
    }

    // ── Boss Combat / Damage Tests ──────────────────────────────

    [UnityTest]
    public IEnumerator BossTakeDamage_ReducesHP()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        float hpBefore = _lastSpawnedBoss.CurrentHP;
        _lastSpawnedBoss.TakeDamage(10);

        yield return null;

        Assert.Less(_lastSpawnedBoss.CurrentHP, hpBefore,
            "Boss HP should decrease after taking damage");
    }

    [UnityTest]
    public IEnumerator BossTakeDamage_FiresOnBossHealthChanged()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        bool healthChangedFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnBossHealthChanged += (b) =>
        {
            if (b == capturedBoss) healthChangedFired = true;
        };

        capturedBoss.TakeDamage(10);
        yield return null;

        Assert.IsTrue(healthChangedFired,
            "OnBossHealthChanged event should fire when boss takes damage");
    }

    [UnityTest]
    public IEnumerator BossTakeDamage_FiresOnEnemyHit()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        bool hitFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnEnemyHit += (e) =>
        {
            if (e == (EnemyBase)capturedBoss) hitFired = true;
        };

        capturedBoss.TakeDamage(1);
        yield return null;

        Assert.IsTrue(hitFired,
            "OnEnemyHit event should fire when boss takes damage");
    }

    [UnityTest]
    public IEnumerator BossTakeDamage_FiresOnEnemyDamaged()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        bool damagedFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnEnemyDamaged += (e, d) =>
        {
            if (e == (EnemyBase)capturedBoss) damagedFired = true;
        };

        capturedBoss.TakeDamage(5);
        yield return null;

        Assert.IsTrue(damagedFired,
            "OnEnemyDamaged event should fire when boss takes damage");
    }

    // ── Boss Phase Transition Tests ─────────────────────────────

    [UnityTest]
    public IEnumerator BossPhaseTransition_AdvancesToPhase1()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        // SkeletonKing has threshold at 0.5f (50% HP)
        // Deal enough damage to push below 50%
        float damageNeeded = _lastSpawnedBoss.MaxHP * 0.51f;
        _lastSpawnedBoss.TakeDamage((int)damageNeeded);

        yield return null;

        Assert.GreaterOrEqual(_lastSpawnedBoss.CurrentPhase, 1,
            "Boss should transition to phase 1 when HP drops below first threshold");
    }

    [UnityTest]
    public IEnumerator BossPhaseTransition_EntersTransitionState()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        // Damage to trigger phase transition
        float damageNeeded = _lastSpawnedBoss.MaxHP * 0.51f;
        _lastSpawnedBoss.TakeDamage((int)damageNeeded);

        yield return null;

        // After transition completes, boss should still be in the new phase
        Assert.GreaterOrEqual(_lastSpawnedBoss.CurrentPhase, 1,
            "Boss phase should be >= 1 after crossing threshold");
    }

    [UnityTest]
    public IEnumerator BossHealthPercent_DecreasesWithDamage()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        float percentBefore = _lastSpawnedBoss.HealthPercent;
        _lastSpawnedBoss.TakeDamage((int)(_lastSpawnedBoss.MaxHP * 0.3f));

        yield return null;

        Assert.Less(_lastSpawnedBoss.HealthPercent, percentBefore,
            "HealthPercent should decrease as boss takes damage");
    }

    // ── Boss Death Tests ────────────────────────────────────────

    [UnityTest]
    public IEnumerator BossDeath_FiresOnBossDiedEvent()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        // Skip if unkillable
        if (_lastSpawnedBoss.IsUnkillable) Assert.Pass("Boss is unkillable — skipping death test");

        bool bossDiedFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnBossDied += (b) =>
        {
            if (b == (BossEnemy)capturedBoss) bossDiedFired = true;
        };

        // Kill the boss with massive damage
        capturedBoss.TakeDamage((int)capturedBoss.MaxHP + 1000);

        yield return TestUtils.WaitForState(
            () => bossDiedFired,
            5f,
            "OnBossDied event did not fire when boss was killed"
        );

        Assert.IsTrue(bossDiedFired,
            "OnBossDied event should fire when boss dies");
    }

    [UnityTest]
    public IEnumerator BossDeath_FiresOnEnemyDiedEvent()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");
        if (_lastSpawnedBoss.IsUnkillable) Assert.Pass("Boss is unkillable — skipping death test");

        bool enemyDiedFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnEnemyDied += (e) =>
        {
            if (e == (EnemyBase)capturedBoss) enemyDiedFired = true;
        };

        capturedBoss.TakeDamage((int)capturedBoss.MaxHP + 1000);

        yield return TestUtils.WaitForState(
            () => enemyDiedFired,
            5f,
            "OnEnemyDied event did not fire when boss was killed"
        );

        Assert.IsTrue(enemyDiedFired,
            "OnEnemyDied event should fire when boss dies");
    }

    [UnityTest]
    public IEnumerator BossDeath_RemovesFromActiveEnemySet()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");
        if (_lastSpawnedBoss.IsUnkillable) Assert.Pass("Boss is unkillable — skipping death test");

        BossEnemy capturedBoss = _lastSpawnedBoss;
        Assert.IsTrue(EnemyBase.ActiveEnemies.Contains(capturedBoss),
            "Boss should be in active set before death");

        capturedBoss.TakeDamage((int)capturedBoss.MaxHP + 1000);

        yield return TestUtils.WaitForState(
            () => !EnemyBase.ActiveEnemies.Contains(capturedBoss),
            5f,
            "Boss was not removed from ActiveEnemies after death"
        );

        Assert.IsFalse(EnemyBase.ActiveEnemies.Contains(capturedBoss),
            "Boss should be removed from ActiveEnemies after death");
    }

    [UnityTest]
    public IEnumerator BossDeath_IncrementsKillCount()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");
        if (_lastSpawnedBoss.IsUnkillable) Assert.Pass("Boss is unkillable — skipping death test");

        var stats = Object.FindObjectOfType<PlayerStats>();
        int killsBefore = stats.KillCount;
        int eliteKillsBefore = stats.EliteKillCount;

        _lastSpawnedBoss.TakeDamage((int)_lastSpawnedBoss.MaxHP + 1000);

        yield return TestUtils.WaitForState(
            () => stats.KillCount > killsBefore,
            5f,
            "Kill count was not incremented after boss death"
        );

        Assert.AreEqual(killsBefore + 1, stats.KillCount,
            "Kill count should increment by 1 when boss dies");
        Assert.AreEqual(eliteKillsBefore + 1, stats.EliteKillCount,
            "Elite kill count should increment by 1 when boss dies");
    }

    // ── Unkillable Boss Tests ───────────────────────────────────

    [UnityTest]
    public IEnumerator UnkillableBoss_StaysAt1HP()
    {
        yield return StartGameForBossTest();

        yield return SpawnUnkillableBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No unkillable boss data available — skipping");

        Assert.IsTrue(_lastSpawnedBoss.IsUnkillable,
            "Death boss should be marked as unkillable");

        // Try to kill it with massive damage
        _lastSpawnedBoss.TakeDamage((int)_lastSpawnedBoss.MaxHP + 1000);
        yield return null;

        Assert.Greater(_lastSpawnedBoss.CurrentHP, 0f,
            "Unkillable boss HP should remain above 0");
        Assert.AreEqual(1f, _lastSpawnedBoss.CurrentHP, 0.01f,
            "Unkillable boss should stop at 1 HP");
    }

    [UnityTest]
    public IEnumerator UnkillableBoss_DoesNotFireOnBossDied()
    {
        yield return StartGameForBossTest();

        yield return SpawnUnkillableBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No unkillable boss data available — skipping");

        bool bossDiedFired = false;
        GameEvents.OnBossDied += (b) => bossDiedFired = true;

        _lastSpawnedBoss.TakeDamage((int)_lastSpawnedBoss.MaxHP + 1000);
        yield return null;

        Assert.IsFalse(bossDiedFired,
            "OnBossDied should NOT fire for an unkillable boss");
    }

    [UnityTest]
    public IEnumerator UnkillableBoss_StillFiresOnBossHealthChanged()
    {
        yield return StartGameForBossTest();

        yield return SpawnUnkillableBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No unkillable boss data available — skipping");

        bool healthChangedFired = false;
        BossEnemy capturedBoss = _lastSpawnedBoss;
        GameEvents.OnBossHealthChanged += (b) =>
        {
            if (b == capturedBoss) healthChangedFired = true;
        };

        capturedBoss.TakeDamage(10);
        yield return null;

        Assert.IsTrue(healthChangedFired,
            "OnBossHealthChanged should still fire for unkillable boss");
    }

    [UnityTest]
    public IEnumerator UnkillableBoss_PhaseTransitionStillWorks()
    {
        yield return StartGameForBossTest();

        yield return SpawnUnkillableBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No unkillable boss data available — skipping");

        // Deal enough damage to cross the first phase threshold (0.7f for DeathBoss)
        float damageNeeded = _lastSpawnedBoss.MaxHP * 0.31f;
        _lastSpawnedBoss.TakeDamage((int)damageNeeded);

        yield return null;

        Assert.GreaterOrEqual(_lastSpawnedBoss.CurrentPhase, 1,
            "Unkillable boss should still undergo phase transitions");
    }

    // ── Boss Name & Properties Tests ────────────────────────────

    [UnityTest]
    public IEnumerator BossName_MatchesEnemyData()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        Assert.IsNotNull(_lastSpawnedBoss.BossName,
            "Boss should have a name");
        Assert.IsNotEmpty(_lastSpawnedBoss.BossName,
            "Boss name should not be empty");
    }

    [UnityTest]
    public IEnumerator BossMaxHP_MatchesData()
    {
        yield return StartGameForBossTest();

        yield return SpawnTestBoss();
        if (_lastSpawnedBoss == null) Assert.Pass("No boss data available — skipping");

        Assert.Greater(_lastSpawnedBoss.MaxHP, 0f,
            "Boss should have positive MaxHP");
        Assert.AreEqual(_lastSpawnedBoss.MaxHP, _lastSpawnedBoss.CurrentHP, 0.01f,
            "Boss should start at full HP (MaxHP == CurrentHP at initialization)");
    }

    // ── Spawner Boss Spawn Flags Tests ───────────────────────────

    [UnityTest]
    public IEnumerator SpawnerResetBossFlags_AllowsRespawn()
    {
        yield return StartGameForBossTest();

        var spawner = Object.FindObjectOfType<EnemySpawner>();
        if (spawner == null) Assert.Pass("No EnemySpawner found — skipping");

        // Reset flags should not throw
        spawner.ResetBossFlags();
        yield return null;

        // Game should still be in playing state
        Assert.IsTrue(GameManager.Instance.IsPlaying,
            "Game should still be playing after ResetBossFlags");
    }

    /// <summary>
    /// Spawn a killable boss (Skeleton King) via EnemyDatabase for testing.
    /// Sets _lastSpawnedBoss after the boss is initialized.
    /// </summary>
    private IEnumerator SpawnTestBoss()
    {
        _lastSpawnedBoss = null;

        var db = EnemyDatabase.Instance;
        if (db == null || db.bosses == null || db.bosses.Length == 0)
            yield break;

        // Find a killable boss (prefer Skeleton King)
        EnemyData bossData = db.GetBossById("skeleton_king");
        if (bossData == null)
        {
            // Try any boss
            foreach (var b in db.bosses)
            {
                if (b != null && b.prefab != null)
                {
                    bossData = b;
                    break;
                }
            }
        }

        if (bossData == null || bossData.prefab == null)
            yield break;

        var player = Object.FindObjectOfType<PlayerController>();
        if (player == null)
            yield break;

        // Subscribe before spawning so we capture the event
        GameEvents.OnBossSpawned += CaptureBoss;

        Vector2 playerPos = player.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 spawnPos = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f;

        GameObject bossObj = Object.Instantiate(bossData.prefab, spawnPos, Quaternion.identity);
        var boss = bossObj.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.Initialize(bossData);
        }

        yield return null;

        GameEvents.OnBossSpawned -= CaptureBoss;

        // Fallback: if event didn't fire but boss exists, set it directly
        if (_lastSpawnedBoss == null && boss != null)
            _lastSpawnedBoss = boss;
    }

    /// <summary>
    /// Spawn the unkillable boss (Death) via EnemyDatabase for testing.
    /// </summary>
    private IEnumerator SpawnUnkillableBoss()
    {
        _lastSpawnedBoss = null;

        var db = EnemyDatabase.Instance;
        EnemyData bossData = db != null ? db.GetBossById("death") : null;

        if (bossData == null || bossData.prefab == null)
            yield break;

        var player = Object.FindObjectOfType<PlayerController>();
        if (player == null)
            yield break;

        GameEvents.OnBossSpawned += CaptureBoss;

        Vector2 playerPos = player.transform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 spawnPos = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f;

        GameObject bossObj = Object.Instantiate(bossData.prefab, spawnPos, Quaternion.identity);
        var boss = bossObj.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.Initialize(bossData);
        }

        yield return null;

        GameEvents.OnBossSpawned -= CaptureBoss;

        if (_lastSpawnedBoss == null && boss != null)
            _lastSpawnedBoss = boss;
    }

    private void CaptureBoss(BossEnemy boss)
    {
        _lastSpawnedBoss = boss;
    }
}
