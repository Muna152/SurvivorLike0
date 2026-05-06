using UnityEngine;

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

    /// <summary>Elapsed play time in the current run (seconds, unscaled).</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>The character selected for the current run.</summary>
    public CharacterData SelectedCharacter { get; private set; }

    // References set by the scene setup; will be properly typed once those scripts exist.
    public MonoBehaviour PlayerControllerRef { get; set; }
    public MonoBehaviour EnemySpawnerRef { get; set; }

    protected override void Awake()
    {
        base.Awake();
        ResetTimeScale();
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
        GameEvents.ClearAll();
        if (DifficultyManager.HasInstance)
            DifficultyManager.Instance.ResetDifficulty();

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
            if (weaponMgr != null) weaponMgr.EquipStartingWeapon();
        }

        Debug.Log($"[GameManager] Game started with character: {(SelectedCharacter != null ? SelectedCharacter.characterName : "none")}");
    }

    /// <summary>Pause the game.</summary>
    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;

        _currentState = GameState.Paused;
        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game paused.");
    }

    /// <summary>Resume from pause.</summary>
    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;

        _currentState = GameState.Playing;
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Game resumed.");
    }

    /// <summary>End the game (player death or victory).</summary>
    public void EndGame(bool victory)
    {
        if (_currentState == GameState.GameOver || _currentState == GameState.Victory) return;

        _currentState = victory ? GameState.Victory : GameState.GameOver;
        Time.timeScale = 0f;

        if (victory)
        {
            Debug.Log($"[GameManager] Victory! Time: {ElapsedTime:F1}s");
        }
        else
        {
            Debug.Log($"[GameManager] Game Over. Survived: {ElapsedTime:F1}s");
        }
    }

    /// <summary>Return to the main menu state.</summary>
    public void ReturnToMenu()
    {
        _currentState = GameState.Menu;
        ResetTimeScale();
        ElapsedTime = 0f;
        Debug.Log("[GameManager] Returned to menu.");
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