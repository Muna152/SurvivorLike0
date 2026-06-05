using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode integration tests for the combat system:
/// enemy spawning, damage, death, and player combat interactions.
/// </summary>
public class CombatTests
{
    [UnityTest]
    public IEnumerator EnemySpawns_AfterGameStarts()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // Wait for enemies to spawn naturally
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount > 0,
            10f,
            "No enemies spawned within 10 seconds of game start"
        );

        Assert.Greater(EnemyBase.ActiveEnemyCount, 0);
    }

    [UnityTest]
    public IEnumerator EnemyTakesDamage_ReducesHP()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount > 0, 10f
        );

        // Get first enemy
        EnemyBase enemy = GetFirstEnemy();
        Assert.IsNotNull(enemy, "Should have at least one enemy");

        float hpBefore = enemy.CurrentHP;
        bool damageFired = false;
        GameEvents.OnEnemyDamaged += (e, d) =>
        {
            if (e == enemy) damageFired = true;
        };

        enemy.TakeDamage(1);
        yield return null;

        Assert.Less(enemy.CurrentHP, hpBefore,
            "Enemy HP should decrease after taking damage");
        Assert.IsTrue(damageFired,
            "OnEnemyDamaged event should fire");
    }

    [UnityTest]
    public IEnumerator EnemyDeath_FiresEventAndIncrementsKillCount()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount > 0, 10f
        );

        EnemyBase enemy = GetFirstEnemy();
        Assert.IsNotNull(enemy, "Should have at least one enemy");

        var stats = Object.FindObjectOfType<PlayerStats>();
        int killsBefore = stats.KillCount;

        bool deathFired = false;
        GameEvents.OnEnemyDied += (e) =>
        {
            if (e == enemy) deathFired = true;
        };

        // Kill the enemy with massive damage
        enemy.TakeDamage((int)enemy.CurrentHP + 1000);

        yield return TestUtils.WaitForState(
            () => deathFired,
            5f,
            "OnEnemyDied event did not fire"
        );

        Assert.AreEqual(killsBefore + 1, stats.KillCount,
            "Kill count should increment when enemy dies");
    }

    [UnityTest]
    public IEnumerator EnemyDeath_RemovesFromActiveSet()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount > 0, 10f
        );

        EnemyBase enemy = GetFirstEnemy();
        int countBefore = EnemyBase.ActiveEnemyCount;

        enemy.TakeDamage((int)enemy.CurrentHP + 1000);

        // Wait for enemy to be removed from active set
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount < countBefore,
            5f,
            "Enemy was not removed from ActiveEnemies after death"
        );

        Assert.Less(EnemyBase.ActiveEnemyCount, countBefore);
    }

    [UnityTest]
    public IEnumerator EnemyHit_FiresOnEnemyHit()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();
        yield return TestUtils.WaitForState(
            () => EnemyBase.ActiveEnemyCount > 0, 10f
        );

        EnemyBase enemy = GetFirstEnemy();
        bool hitFired = false;
        GameEvents.OnEnemyHit += (e) =>
        {
            if (e == enemy) hitFired = true;
        };

        enemy.TakeDamage(1);
        yield return null;

        Assert.IsTrue(hitFired, "OnEnemyHit event should fire on damage");
    }

    [UnityTest]
    public IEnumerator PlayerTakeDamage_ReducesHP()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        float hpBefore = stats.CurrentHP;

        stats.TakeDamage(10);
        yield return null;

        Assert.Less(stats.CurrentHP, hpBefore,
            "Player HP should decrease after taking damage");
    }

    [UnityTest]
    public IEnumerator PlayerHeal_IncreasesHP()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();

        // First, damage the player
        stats.TakeDamage(20);
        yield return null;
        float hpAfterDamage = stats.CurrentHP;

        // Then heal
        bool healFired = false;
        GameEvents.OnPlayerHealed += (amt) => healFired = true;
        stats.Heal(10);
        yield return null;

        Assert.AreEqual(hpAfterDamage + 10, stats.CurrentHP, 0.01f,
            "Player HP should increase after healing");
        Assert.IsTrue(healFired, "OnPlayerHealed event should fire");
    }

    [UnityTest]
    public IEnumerator PlayerTakeDamage_AppliesArmor()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        float hpBefore = stats.CurrentHP;
        int armor = stats.Armor;

        stats.TakeDamage(30);
        yield return null;

        int expectedDamage = System.Math.Max(1, 30 - armor);
        Assert.AreEqual(hpBefore - expectedDamage, stats.CurrentHP, 0.01f,
            $"With armor={armor}, TakeDamage(30) should deal {expectedDamage}");
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static EnemyBase GetFirstEnemy()
    {
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null) return enemy;
        }
        return null;
    }
}
