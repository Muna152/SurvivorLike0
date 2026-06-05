using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode integration tests for the core game flow:
/// startup → playing → death → game over → return to menu.
/// </summary>
public class GameFlowTests
{
    [SetUp]
    public void SetUp()
    {
        TestUtils.CleanupStatics();
    }

    [TearDown]
    public void TearDown()
    {
        TestUtils.CleanupStatics();
    }

    // ── Startup ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator StartGame_EntersPlayingState()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        Assert.AreEqual(GameManager.GameState.Playing,
            GameManager.Instance.CurrentState);
    }

    [UnityTest]
    public IEnumerator StartGame_PlayerExistsAndAlive()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var player = Object.FindObjectOfType<PlayerController>();
        Assert.IsNotNull(player, "PlayerController should exist in scene");

        var stats = Object.FindObjectOfType<PlayerStats>();
        Assert.IsNotNull(stats, "PlayerStats should exist in scene");
        Assert.Greater(stats.CurrentHP, 0f, "Player should be alive");
    }

    [UnityTest]
    public IEnumerator StartGame_ElapsedTimeStartsCounting()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // Wait a short real-time duration
        yield return new WaitForSecondsRealtime(0.5f);

        Assert.Greater(GameManager.Instance.ElapsedTime, 0f,
            "Elapsed time should be increasing during gameplay");
    }

    [UnityTest]
    public IEnumerator StartGame_TimeScaleIsOne()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        Assert.AreEqual(1f, Time.timeScale, 0.001f,
            "Time.timeScale should be 1 during gameplay");
    }

    // ── Death ───────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator PlayerDeath_TransitionsToGameOver()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        stats.TakeDamage((int)stats.CurrentHP + 100);

        yield return TestUtils.WaitForState(
            () => GameManager.Instance.CurrentState == GameManager.GameState.GameOver,
            5f,
            "Game did not enter GameOver after player death"
        );

        Assert.AreEqual(GameManager.GameState.GameOver,
            GameManager.Instance.CurrentState);
    }

    [UnityTest]
    public IEnumerator PlayerDeath_FiresOnPlayerDied()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        bool died = false;
        GameEvents.OnPlayerDied += () => died = true;

        stats.TakeDamage((int)stats.CurrentHP + 100);

        yield return TestUtils.WaitForState(
            () => died,
            5f,
            "OnPlayerDied event did not fire"
        );

        Assert.IsTrue(died);
    }

    [UnityTest]
    public IEnumerator PlayerDeath_SetsTimeScaleToZero()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        stats.TakeDamage((int)stats.CurrentHP + 100);

        yield return TestUtils.WaitForState(
            () => GameManager.Instance.CurrentState == GameManager.GameState.GameOver,
            5f
        );

        Assert.AreEqual(0f, Time.timeScale, 0.001f,
            "Time.timeScale should be 0 on game over");
    }

    // ── Return to Menu ───────────────────────────────────────────

    [UnityTest]
    public IEnumerator ReturnToMenu_EntersMenuState()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.ReturnToMenu();
        yield return null;

        Assert.AreEqual(GameManager.GameState.Menu,
            GameManager.Instance.CurrentState);
    }

    [UnityTest]
    public IEnumerator ReturnToMenu_ResetsTimeScale()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.ReturnToMenu();
        yield return null;

        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }

    [UnityTest]
    public IEnumerator ReturnToMenu_ResetsElapsedTime()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();
        yield return new WaitForSecondsRealtime(0.5f);

        GameManager.Instance.ReturnToMenu();
        yield return null;

        Assert.AreEqual(0f, GameManager.Instance.ElapsedTime, 0.01f);
    }

    // ── Game Over → Return to Menu ──────────────────────────────

    [UnityTest]
    public IEnumerator GameOver_ThenReturnToMenu_EntersMenuState()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        var stats = Object.FindObjectOfType<PlayerStats>();
        stats.TakeDamage((int)stats.CurrentHP + 100);

        yield return TestUtils.WaitForState(
            () => GameManager.Instance.CurrentState == GameManager.GameState.GameOver,
            5f
        );

        GameManager.Instance.ReturnToMenu();
        yield return null;

        Assert.AreEqual(GameManager.GameState.Menu,
            GameManager.Instance.CurrentState);
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }
}
