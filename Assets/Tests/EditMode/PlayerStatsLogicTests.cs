using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode unit tests for PlayerStats pure logic.
/// Creates a temporary GameObject with PlayerStats for each test.
/// </summary>
public class PlayerStatsLogicTests
{
    private GameObject _go;
    private PlayerStats _stats;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestPlayerStats");
        _stats = _go.AddComponent<PlayerStats>();
        // Set known starting values (bypass InitializeFromCharacterData which needs GameManager)
        _stats.MaxHP = 100f;
        // Access _currentHP via reflection since it's serialized-private
        SetCurrentHP(_stats, _stats.MaxHP);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    // ── HP / Damage ─────────────────────────────────────────────

    [Test]
    public void TakeDamage_ReducesHP()
    {
        _stats.Armor = 0;
        _stats.TakeDamage(20);

        Assert.AreEqual(80f, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void TakeDamage_AppliesArmor_Minimum1()
    {
        _stats.Armor = 5;
        _stats.TakeDamage(10);

        // 10 - 5 = 5 damage
        Assert.AreEqual(95f, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void TakeDamage_ArmorCannotReduceBelow1Damage()
    {
        _stats.Armor = 100;
        _stats.TakeDamage(10);

        // Max(1, 10 - 100) = 1 damage
        Assert.AreEqual(99f, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void TakeDamage_FiresOnPlayerDamaged()
    {
        int receivedDamage = 0;
        GameEvents.OnPlayerDamaged += (d) => receivedDamage = d;

        _stats.Armor = 0;
        _stats.TakeDamage(30);

        Assert.AreEqual(30, receivedDamage);
    }

    [Test]
    public void TakeDamage_WithExtraLives_RevivesInsteadOfDeath()
    {
        _stats.ExtraLives = 1;
        int deathCount = 0;
        GameEvents.OnPlayerDied += () => deathCount++;

        _stats.Armor = 0;
        _stats.TakeDamage(200);

        // Should revive, not die
        Assert.AreEqual(0, deathCount, "Player should not die with extra lives");
        Assert.AreEqual(0, _stats.ExtraLives, "Extra lives should be consumed");
        Assert.Greater(_stats.CurrentHP, 0f, "Player should have revived HP");
    }

    [Test]
    public void TakeDamage_WithoutExtraLives_FiresOnPlayerDied()
    {
        int deathCount = 0;
        GameEvents.OnPlayerDied += () => deathCount++;

        _stats.Armor = 0;
        _stats.TakeDamage(200);

        Assert.AreEqual(1, deathCount);
        Assert.AreEqual(0f, _stats.CurrentHP, 0.01f);
    }

    // ── Heal ────────────────────────────────────────────────────

    [Test]
    public void Heal_IncreasesHP()
    {
        SetCurrentHP(_stats, 50f);
        _stats.Heal(30);

        Assert.AreEqual(80f, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void Heal_ClampsToMaxHP()
    {
        SetCurrentHP(_stats, 90f);
        _stats.Heal(30);

        Assert.AreEqual(_stats.MaxHP, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void Heal_FiresOnPlayerHealed()
    {
        SetCurrentHP(_stats, 50f);
        float healedAmount = 0f;
        GameEvents.OnPlayerHealed += (amt) => healedAmount = amt;

        _stats.Heal(20);

        Assert.AreEqual(20f, healedAmount, 0.01f);
    }

    [Test]
    public void Heal_AtFullHP_DoesNotFireEvent()
    {
        float healedAmount = -1f;
        GameEvents.OnPlayerHealed += (amt) => healedAmount = amt;

        _stats.Heal(10);

        Assert.AreEqual(-1f, healedAmount, "Heal at full HP should not fire event");
    }

    // ── EXP / Level ─────────────────────────────────────────────

    [Test]
    public void AddEXP_IncrementsCurrentEXP()
    {
        _stats.AddEXP(5);

        Assert.AreEqual(5f, _stats.CurrentEXP, 0.01f);
    }

    [Test]
    public void AddEXP_EnoughToLevelUp_IncrementsLevel()
    {
        // Level 1: EXPToNextLevel = 5 + 2*1*1 = 7
        int levelBefore = _stats.Level;

        _stats.AddEXP(10); // 10 >= 7, should level up

        Assert.AreEqual(levelBefore + 1, _stats.Level);
    }

    [Test]
    public void AddEXP_LevelUpCarriesOverOverflow()
    {
        // Level 1: EXPToNextLevel = 7
        _stats.AddEXP(10);

        // Overflow: 10 - 7 = 3 remaining EXP
        Assert.AreEqual(3f, _stats.CurrentEXP, 0.01f);
    }

    [Test]
    public void AddEXP_MultipleLevelUpsAtOnce()
    {
        // Level 1 → 7, Level 2 → 5+8=13, Level 3 → 5+18=23
        // Total to reach level 4: 7+13+23 = 43
        _stats.AddEXP(50);

        Assert.GreaterOrEqual(_stats.Level, 4);
    }

    [Test]
    public void AddEXP_FiresOnPlayerLevelUp()
    {
        int newLevel = 0;
        GameEvents.OnPlayerLevelUp += (lvl) => newLevel = lvl;

        _stats.AddEXP(10);

        Assert.AreEqual(2, newLevel);
    }

    [Test]
    public void EXPToNextLevel_Formula()
    {
        // 5 + 2 * level^2
        Assert.AreEqual(7f, _stats.EXPToNextLevel, 0.01f); // Level 1

        // Force to level 2
        _stats.AddEXP(100);
        // Now level >= 2, check formula
        int currentLevel = _stats.Level;
        float expected = 5 + 2 * currentLevel * currentLevel;
        Assert.AreEqual(expected, _stats.EXPToNextLevel, 0.01f);
    }

    // ── Kill / Gold ─────────────────────────────────────────────

    [Test]
    public void AddKill_IncrementsKillCount()
    {
        _stats.AddKill();
        _stats.AddKill();
        _stats.AddKill();

        Assert.AreEqual(3, _stats.KillCount);
    }

    [Test]
    public void AddEliteKill_IncrementsEliteKillCount()
    {
        _stats.AddEliteKill();

        Assert.AreEqual(1, _stats.EliteKillCount);
    }

    [Test]
    public void AddGold_IncrementsGold()
    {
        _stats.AddGold(50);

        Assert.AreEqual(50, _stats.Gold);
    }

    [Test]
    public void AddGold_FiresOnGoldChanged()
    {
        int newGold = 0;
        GameEvents.OnGoldChanged += (g) => newGold = g;

        _stats.AddGold(25);

        Assert.AreEqual(25, newGold);
    }

    // ── ClampCurrentHP ──────────────────────────────────────────

    [Test]
    public void ClampCurrentHP_ClampsToMaxHP()
    {
        SetCurrentHP(_stats, 200f);
        _stats.ClampCurrentHP();

        Assert.AreEqual(_stats.MaxHP, _stats.CurrentHP, 0.01f);
    }

    [Test]
    public void ClampCurrentHP_ClampsToZero()
    {
        SetCurrentHP(_stats, -10f);
        _stats.ClampCurrentHP();

        Assert.AreEqual(0f, _stats.CurrentHP, 0.01f);
    }

    // ── Passive Items ───────────────────────────────────────────

    [Test]
    public void HasPassive_ReturnsFalse_WhenNotApplied()
    {
        Assert.IsFalse(_stats.HasPassive(null));
    }

    [Test]
    public void GetPassiveLevel_ReturnsZero_WhenNotApplied()
    {
        Assert.AreEqual(0, _stats.GetPassiveLevel(null));
    }

    [Test]
    public void ApplyPassive_DoesNothing_WhenNull()
    {
        // Should not throw
        _stats.ApplyPassive(null);
    }

    [Test]
    public void RemovePassive_DoesNothing_WhenNull()
    {
        // Should not throw
        _stats.RemovePassive(null);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static void SetCurrentHP(PlayerStats stats, float value)
    {
        var field = typeof(PlayerStats).GetField(
            "_currentHP",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Assert.IsNotNull(field, "_currentHP field not found via reflection");
        field.SetValue(stats, value);
    }
}
