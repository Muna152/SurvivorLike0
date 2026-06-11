using UnityEngine;
using System;

/// <summary>
/// Manages the single-run game lifecycle: Menu → Playing → Paused/GameOver/Victory.
/// Controls Time.timeScale and holds references to key game systems.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    [Header("Current State")]
    [SerializeField] private GameState _currentState = GameState.Menu;

    /// <summary>Current game state (read-only for external consumers).</summary>
    public GameState CurrentState => _currentState;

    /// <summary>Event fired when the game state changes.</summary>
    public event Action<GameState> OnGameStateChanged;

    /// <summary>Elapsed play time in the current run (seconds, unscaled).</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>The character selected for the current run.</summary>
    public CharacterData SelectedCharacter { get; private set; }

    /// <summary>Character to auto-start with after scene reload (used by Retry).</summary>
    public CharacterData PendingAutoStart { get; set; }

    // References set by the scene setup; will be properly typed once those scripts exist.
    public MonoBehaviour PlayerControllerRef { get; set; }
    public MonoBehaviour EnemySpawnerRef { get; set; }

    // ── Reference-counted pause system ───────────────────────────
    // Multiple systems (upgrade UI, chest open, pause menu) may each need
    // timeScale=0.  PushPause / PopPause tracks depth so that no caller
    // accidentally resumes the game while another still needs it paused.
    private int _pauseDepth;

    /// <summary>True when any system has pushed a pause.</summary>
    public bool IsSystemPaused => _pauseDepth > 0;

    /// <summary>
    /// Request a pause.  Increments the depth counter and sets timeScale=0.
    /// Every PushPause MUST be paired with a PopPause.
    /// </summary>
    public void PushPause()
    {
        _pauseDepth++;
        if (_pauseDepth == 1)
            Time.timeScale = 0f;
    }

    /// <summary>
    /// Release a pause.  Decrements the depth counter; only sets timeScale=1
    /// when all pauses have been released (depth reaches 0).
    /// </summary>
    public void PopPause()
    {
        if (_pauseDepth <= 0)
        {
            DebugLogger.LogWarning("[GameManager] PopPause called with no matching PushPause.");
            return;
        }
        _pauseDepth--;
        if (_pauseDepth == 0)
            Time.timeScale = 1f;
    }

    protected override void Awake()
    {
        base.Awake();
        ResetTimeScale();
        // Ensure AudioManager is created for BGM on startup
        _ = AudioManager.Instance;
        // Ensure VFXManager is created for VFX on startup
        _ = VFXManager.Instance;
    }

    // ── Lifecycle Methods ───────────────────────────────────────

    /// <summary>Begin a new game run with the selected character.</summary>
    public void StartGame(CharacterData character)
    {
        SelectedCharacter = character;
        StartGame();
    }

    /// <summary>Begin a new game run (uses previously selected character or default).</summary>
    public void StartGame()
    {
        ElapsedTime = 0f;
        _currentState = GameState.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(_currentState);

        // Reset difficulty for new run
        if (DifficultyManager.HasInstance)
            DifficultyManager.Instance.ResetDifficulty();

        // Reset boss spawn flags for new run
        var spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.ResetBossFlags();

        // Activate the player if they were disabled during menu
        var player = FindObjectOfType<PlayerController>();
        if (player != null && !player.gameObject.activeSelf)
            player.gameObject.SetActive(true);

        // Initialize player stats and starting weapon from the selected character
        if (SelectedCharacter != null)
        {
            var stats = FindObjectOfType<PlayerStats>();
            if (stats != null) stats.InitializeFromCharacterData();

            var weaponMgr = FindObjectOfType<PlayerWeaponManager>();
            if (weaponMgr != null)             weaponMgr.EquipStartingWeapon();
        }

        DebugLogger.Log($"[GameManager] Game started with character: {(SelectedCharacter != null ? SelectedCharacter.characterName : "none")}");

        // Ensure HUD elements are visible for gameplay
        // (MainMenuUI.Show() hides them; this re-enables regardless of start flow)
        var mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu != null)
            mainMenu.SetHUDEnabled(true);

        if (AudioManager.HasInstance)
            AudioManager.Instance.PlayBattleBGM();

        // Start tutorial on first game session
        if (TutorialManager.HasInstance && !TutorialManager.Instance.IsTutorialCompleted)
            TutorialManager.Instance.StartTutorial();
    }

    /// <summary>Pause the game.</summary>
    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;

        _currentState = GameState.Paused;
        PushPause();
        GameEvents.InvokeGamePaused();
        OnGameStateChanged?.Invoke(_currentState);
        DebugLogger.Log("[GameManager] Game paused.");
    }

    /// <summary>Resume from pause.</summary>
    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;

        _currentState = GameState.Playing;
        PopPause();
        GameEvents.InvokeGameResumed();
        OnGameStateChanged?.Invoke(_currentState);
        DebugLogger.Log("[GameManager] Game resumed.");
    }

    /// <summary>End the game (player death or victory).</summary>
    public void EndGame(bool victory)
    {
        if (_currentState == GameState.GameOver || _currentState == GameState.Victory) return;

        _currentState = victory ? GameState.Victory : GameState.GameOver;
        _pauseDepth = 0;
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(_currentState);

        if (victory)
        {
            DebugLogger.Log($"[GameManager] Victory! Time: {ElapsedTime:F1}s");
        }
        else
        {
            DebugLogger.Log($"[GameManager] Game Over. Survived: {ElapsedTime:F1}s");
        }
    }

    /// <summary>Return to the main menu state.</summary>
    public void ReturnToMenu()
    {
        _currentState = GameState.Menu;
        _pauseDepth = 0;
        ResetTimeScale();
        ElapsedTime = 0f;
        SelectedCharacter = null;
        OnGameStateChanged?.Invoke(_currentState);

        // Clean up all game session state to prevent memory leaks
        CleanupSessionState();

        if (AudioManager.HasInstance)
            AudioManager.Instance.PlayMenuBGM();

        DebugLogger.Log("[GameManager] Returned to menu.");
    }

    /// <summary>
    /// Clean up all game session state: events, static collections, pools.
    /// Prevents memory leaks across multiple game sessions.
    /// </summary>
    private void CleanupSessionState()
    {
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();

        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        AreaWeapon.ClearStaticCache();
        WeaponEvolutionVFX.ClearStaticCache();
    }

    // ── Update ─────────────────────────────────────────────────

    private void Update()
    {
        if (_currentState == GameState.Playing)
        {
            ElapsedTime += Time.unscaledDeltaTime;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────

    private void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    /// <summary>Convenience check: is the game currently in a playable state?</summary>
    public bool IsPlaying => _currentState == GameState.Playing;

    /// <summary>Convenience check: is the game paused?</summary>
    public bool IsPaused => _currentState == GameState.Paused;

    /// <summary>Convenience check: has the game ended (win or lose)?</summary>
    public bool IsGameOver => _currentState == GameState.GameOver || _currentState == GameState.Victory;
}