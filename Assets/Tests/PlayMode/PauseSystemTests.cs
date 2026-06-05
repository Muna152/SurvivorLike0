using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for the reference-counted pause system (PushPause/PopPause).
/// This is a critical system that multiple game features depend on
/// (upgrade UI, chest opening, pause menu), so correct depth tracking is essential.
/// </summary>
public class PauseSystemTests
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

    [UnityTest]
    public IEnumerator PushPause_SetsTimeScaleToZero()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.PushPause();

        Assert.IsTrue(GameManager.Instance.IsSystemPaused,
            "IsSystemPaused should be true after PushPause");
        Assert.AreEqual(0f, Time.timeScale, 0.001f,
            "Time.timeScale should be 0 after PushPause");
    }

    [UnityTest]
    public IEnumerator PopPause_RestoresTimeScale()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.PushPause();
        Assert.AreEqual(0f, Time.timeScale, 0.001f);

        GameManager.Instance.PopPause();

        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "IsSystemPaused should be false after matching PopPause");
        Assert.AreEqual(1f, Time.timeScale, 0.001f,
            "Time.timeScale should be 1 after matching PopPause");
    }

    [UnityTest]
    public IEnumerator PushPause_Twice_RequiresTwoPops()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // Push twice (simulating upgrade UI + chest both open)
        GameManager.Instance.PushPause();
        GameManager.Instance.PushPause();

        Assert.IsTrue(GameManager.Instance.IsSystemPaused);
        Assert.AreEqual(0f, Time.timeScale, 0.001f);

        // First pop — should still be paused
        GameManager.Instance.PopPause();
        Assert.IsTrue(GameManager.Instance.IsSystemPaused,
            "After Push×2 + Pop×1: should still be paused (depth=1)");

        // Second pop — should now resume
        GameManager.Instance.PopPause();
        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "After Push×2 + Pop×2: should be resumed (depth=0)");
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }

    [UnityTest]
    public IEnumerator PopPause_WithNoPush_WarnsButDoesNotCrash()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // PopPause with no matching Push should not throw
        GameManager.Instance.PopPause();

        Assert.IsFalse(GameManager.Instance.IsSystemPaused);
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }

    [UnityTest]
    public IEnumerator PauseGame_EntersPausedState()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.PauseGame();

        Assert.AreEqual(GameManager.GameState.Paused,
            GameManager.Instance.CurrentState);
        Assert.IsTrue(GameManager.Instance.IsSystemPaused);
    }

    [UnityTest]
    public IEnumerator ResumeGame_ReturnsToPlayingState()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        GameManager.Instance.PauseGame();
        yield return null;

        GameManager.Instance.ResumeGame();
        yield return null;

        Assert.AreEqual(GameManager.GameState.Playing,
            GameManager.Instance.CurrentState);
        Assert.IsFalse(GameManager.Instance.IsSystemPaused);
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }

    [UnityTest]
    public IEnumerator EndGame_ResetsPauseDepth()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // Push some pauses
        GameManager.Instance.PushPause();
        GameManager.Instance.PushPause();
        Assert.IsTrue(GameManager.Instance.IsSystemPaused);

        // End game should reset everything
        GameManager.Instance.EndGame(false);
        yield return null;

        Assert.AreEqual(GameManager.GameState.GameOver,
            GameManager.Instance.CurrentState);
        Assert.AreEqual(0f, Time.timeScale, 0.001f,
            "Game over should set timeScale to 0");
    }

    [UnityTest]
    public IEnumerator ReturnToMenu_ResetsPauseDepth()
    {
        yield return TestUtils.StartGameWithDefaultCharacter();

        // Push a pause that we "forget" to pop (simulating a bug)
        GameManager.Instance.PushPause();

        // ReturnToMenu should clean up
        GameManager.Instance.ReturnToMenu();
        yield return null;

        Assert.IsFalse(GameManager.Instance.IsSystemPaused,
            "ReturnToMenu should reset pause depth");
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }
}
