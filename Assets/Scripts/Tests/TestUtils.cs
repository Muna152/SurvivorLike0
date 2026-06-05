using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared test infrastructure for PlayMode integration tests.
/// Provides helpers to start the game, wait for states, force level-ups,
/// and clean up static/singleton state between tests.
/// </summary>
public static class TestUtils
{
    public const string GameSceneName = "GameLevel";

    // ── Game Startup ────────────────────────────────────────────

    /// <summary>
    /// Loads the game scene, ensures a save slot exists, and starts
    /// a game run with the first unlocked character.
    /// After returning, the game is in Playing state with all systems initialized.
    /// </summary>
    public static IEnumerator StartGameWithDefaultCharacter(
        string sceneName = GameSceneName)
    {
        // Load the game scene
        yield return SceneManager.LoadSceneAsync(sceneName);

        // Wait for core singletons to initialize
        yield return WaitForState(
            () => GameManager.HasInstance, 10f,
            "GameManager did not initialize after scene load"
        );
        yield return WaitForState(
            () => UnlockManager.HasInstance, 10f,
            "UnlockManager did not initialize after scene load"
        );

        // Ensure a save slot exists (UnlockManager depends on it)
        EnsureSaveSlot();

        // Find an unlocked character
        var character = GetFirstUnlockedCharacter();
        Assert.IsNotNull(character, "No unlocked character available for testing");

        // Start the game
        GameManager.Instance.StartGame(character);

        // Wait for Playing state
        yield return WaitForState(
            () => GameManager.Instance.CurrentState == GameManager.GameState.Playing,
            10f,
            "Game did not enter Playing state after StartGame"
        );

        // Extra frame for all initializations (weapons, stats, etc.)
        yield return null;
    }

    // ── Wait Helpers ────────────────────────────────────────────

    /// <summary>
    /// Wait for a condition to become true, with a timeout.
    /// Uses real time so it works even when Time.timeScale = 0.
    /// </summary>
    public static IEnumerator WaitForState(
        System.Func<bool> condition,
        float timeout = 5f,
        string message = "Wait timed out")
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > timeout)
            {
                Assert.Fail(message);
                yield break;
            }
            yield return null;
        }
    }

    // ── Level Up ────────────────────────────────────────────────

    /// <summary>
    /// Forces the player to level up by adding enough EXP.
    /// Returns after the level-up event fires.
    /// </summary>
    public static IEnumerator ForceLevelUp(PlayerStats stats)
    {
        bool levelUpFired = false;

        System.Action<int> handler = (_) => levelUpFired = true;
        GameEvents.OnPlayerLevelUp += handler;

        try
        {
            stats.AddEXP((int)stats.EXPToNextLevel + 100);

            yield return WaitForState(
                () => levelUpFired,
                3f,
                "Level-up event did not fire after adding EXP"
            );
        }
        finally
        {
            GameEvents.OnPlayerLevelUp -= handler;
        }
    }

    // ── Character / Save Slot ──────────────────────────────────

    /// <summary>
    /// Returns the first unlocked character from CharacterDatabase.
    /// </summary>
    public static CharacterData GetFirstUnlockedCharacter()
    {
        if (!UnlockManager.HasInstance) return null;

        var characters = UnlockManager.Instance.AllCharacters;
        if (characters == null) return null;

        foreach (var c in characters)
        {
            if (c != null && UnlockManager.Instance.IsUnlocked(c.id))
                return c;
        }
        return null;
    }

    /// <summary>
    /// Ensures a save slot exists so UnlockManager can read unlock data.
    /// If no active slot, creates one named "TestSlot".
    /// </summary>
    public static void EnsureSaveSlot()
    {
        if (SaveSlotManager.HasActiveSlot) return;

        // Find an empty slot index
        for (int i = 0; i < SaveSlotManager.MAX_SLOTS; i++)
        {
            if (!SaveSlotManager.HasSlot(i))
            {
                SaveSlotManager.CreateSlot("TestSlot");
                return;
            }
        }

        // All slots full — use the first one
        SaveSlotManager.SwitchSlot(0);
    }

    // ── Cleanup ────────────────────────────────────────────────

    /// <summary>
    /// Cleans up all static and singleton state between tests.
    /// Call this in [TearDown] or [OneTimeTearDown].
    /// </summary>
    public static void CleanupStatics()
    {
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();
        Time.timeScale = 1f;

        // Force-reset GameManager's pause depth if it exists
        if (GameManager.HasInstance)
        {
            var gm = GameManager.Instance;
            gm.ReturnToMenu();

            // Reset _pauseDepth via reflection (field is private)
            var pauseDepthField = typeof(GameManager).GetField(
                "_pauseDepth",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (pauseDepthField != null)
                pauseDepthField.SetValue(gm, 0);
        }
    }
}
