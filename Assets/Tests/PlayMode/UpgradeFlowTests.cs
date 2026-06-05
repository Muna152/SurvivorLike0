using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode integration tests for the upgrade flow:
/// level-up → pause → options generated → select → resume.
/// </summary>
public class UpgradeFlowTests
{
    [UnityTest]
    public IEnumerator LevelUp_PausesGameAndGeneratesOptions()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        Assert.IsNotNull(upgradeMgr, "UpgradeManager should exist in scene");

        // Capture upgrade options via event
        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        // Wait for options to be generated
        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f,
            "Upgrade options were not generated after level-up"
        );

        // Game should be paused during upgrade selection
        Assert.IsTrue(GameManager.Instance.IsSystemPaused,
            "Game should be paused during upgrade selection");
    }

    [UnityTest]
    public IEnumerator LevelUp_GeneratesThreeOptions()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f
        );

        Assert.AreEqual(3, generatedOptions.Count,
            "Level-up should generate 3 upgrade options");
    }

    [UnityTest]
    public IEnumerator SelectUpgrade_ResumesGame()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f
        );

        // Select the first option
        upgradeMgr.OnOptionSelected(generatedOptions[0]);
        yield return null;

        // Game should resume
        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "Game should resume after selecting an upgrade");
        Assert.AreEqual(1f, Time.timeScale, 0.001f,
            "Time.timeScale should be 1 after upgrade selection");
    }

    [UnityTest]
    public IEnumerator SelectUpgrade_PauseDepthReturnsToZero()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f
        );

        // Before selection: paused
        Assert.IsTrue(GameManager.Instance.IsSystemPaused);

        // Select
        upgradeMgr.OnOptionSelected(generatedOptions[0]);
        yield return null;

        // After selection: not paused (pause depth back to 0)
        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "Pause depth should be 0 after upgrade selection");
    }

    [UnityTest]
    public IEnumerator NewWeaponUpgrade_EquipsWeapon()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        var weaponMgr = Object.FindObjectOfType<PlayerWeaponManager>();

        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f
        );

        // Find a NewWeaponOption if available
        var newWeaponOpt = generatedOptions.Find(o => o is NewWeaponOption);
        if (newWeaponOpt == null)
        {
            // No NewWeaponOption in this batch — skip test gracefully
            // Apply any option to resume the game
            upgradeMgr.OnOptionSelected(generatedOptions[0]);
            yield return null;
            Assert.Pass("No NewWeaponOption in this batch — test skipped gracefully");
            yield break;
        }

        int weaponsBefore = weaponMgr.EquippedWeapons.Count;
        upgradeMgr.OnOptionSelected(newWeaponOpt);
        yield return null;

        Assert.AreEqual(weaponsBefore + 1, weaponMgr.EquippedWeapons.Count,
            "NewWeaponOption should increase weapon count by 1");
    }

    [UnityTest]
    public IEnumerator MultipleLevelUps_QueuesUpgrades()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        var stats = Object.FindObjectOfType<PlayerStats>();

        int optionBatchCount = 0;
        upgradeMgr.OnOptionsGenerated += (opts) => optionBatchCount++;

        // Give enough EXP to level up multiple times
        // Level 1 → 2: 7 EXP, Level 2 → 3: 13 EXP = 20 EXP total
        stats.AddEXP(50);

        // Wait for at least 2 batches of upgrade options
        yield return TestUtils.WaitForState(
            () => optionBatchCount >= 2,
            5f,
            "Multiple level-ups should generate multiple upgrade option batches"
        );

        Assert.GreaterOrEqual(optionBatchCount, 2);
    }

    [UnityTest]
    public IEnumerator SkipUpgrade_ResumesGame()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var upgradeMgr = Object.FindObjectOfType<UpgradeManager>();
        List<UpgradeOption> generatedOptions = null;
        upgradeMgr.OnOptionsGenerated += (opts) => generatedOptions = opts;

        var stats = Object.FindObjectOfType<PlayerStats>();
        yield return TestUtils.ForceLevelUp(stats);

        yield return TestUtils.WaitForState(
            () => generatedOptions != null,
            3f
        );

        // Skip the upgrade instead of selecting one
        upgradeMgr.SkipUpgrade();
        yield return null;

        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "Game should resume after skipping upgrade");
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }
}
