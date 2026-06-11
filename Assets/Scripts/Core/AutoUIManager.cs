using System.Collections;
using UnityEngine;

/// <summary>
/// Automatic UI navigation manager for fully automated testing.
/// Automatically navigates through UI screens when Auto-Pilot is enabled:
/// - MainMenuUI: Auto-select save slot and start game
/// - CharacterSelectUI: Auto-select character and start battle
/// - ResultScreen: Auto-click "Retry" to restart
/// </summary>
public class AutoUIManager : Singleton<AutoUIManager>
{
    [Header("Auto-Navigation Settings")]
    [SerializeField] private float _menuNavigationDelay = 0.5f;
    [SerializeField] private float _characterSelectDelay = 0.5f;
    [SerializeField] private float _resultScreenDelay = 1.0f;

    private Coroutine _pendingNavigation;

    protected override void Awake()
    {
        base.Awake();

        // Ensure this singleton persists across scene loads
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe to UI state events
        GameEvents.OnAutoPilotToggled += HandleAutoPilotToggled;

        // Subscribe to game state events
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        GameEvents.OnAutoPilotToggled -= HandleAutoPilotToggled;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void Start()
    {
        // Auto-navigate if auto-pilot is enabled and we're in menu state
        if (PlayerController.IsAutoPilot && GameManager.HasInstance)
        {
            CheckAndNavigate();
        }
    }

    private void HandleAutoPilotToggled(bool enabled)
    {
        if (enabled)
        {
            Debug.Log("[AutoUI] Auto-Pilot enabled, checking for navigation opportunities...");
            CheckAndNavigate();
        }
        else
        {
            // Cancel any pending navigation when auto-pilot is disabled
            if (_pendingNavigation != null)
            {
                StopCoroutine(_pendingNavigation);
                _pendingNavigation = null;
                Debug.Log("[AutoUI] Auto-Pilot disabled, pending navigation cancelled");
            }
        }
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (!PlayerController.IsAutoPilot) return;

        Debug.Log($"[AutoUI] Game state changed to: {newState}");
        CheckAndNavigate();
    }

    private void CheckAndNavigate()
    {
        if (!PlayerController.IsAutoPilot) return;
        if (!GameManager.HasInstance) return;

        var state = GameManager.Instance.CurrentState;

        switch (state)
        {
            case GameManager.GameState.Menu:
                NavigateMainMenu();
                break;

            case GameManager.GameState.Playing:
                // No navigation needed during gameplay
                break;

            case GameManager.GameState.Paused:
                // No navigation needed during pause
                break;
        }
    }

    #region MainMenu Navigation

    private void NavigateMainMenu()
    {
        // Cancel any existing navigation
        if (_pendingNavigation != null)
        {
            StopCoroutine(_pendingNavigation);
        }

        _pendingNavigation = StartCoroutine(NavigateMainMenuCoroutine());
    }

    private IEnumerator NavigateMainMenuCoroutine()
    {
        Debug.Log("[AutoUI] Starting main menu navigation...");

        yield return new WaitForSecondsRealtime(_menuNavigationDelay);

        var mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu == null)
        {
            Debug.LogWarning("[AutoUI] MainMenuUI not found, skipping navigation");
            _pendingNavigation = null;
            yield break;
        }

        // Step 1: Auto-select save slot if none is active
        if (!SaveSlotManager.HasActiveSlot)
        {
            Debug.Log("[AutoUI] No active save slot, selecting first available slot...");

            for (int i = 0; i < SaveSlotManager.MAX_SLOTS; i++)
            {
                if (SaveSlotManager.HasSlot(i))
                {
                    SaveSlotManager.SwitchSlot(i);
                    Debug.Log($"[AutoUI] Selected save slot {i}: {SaveSlotManager.GetSlotName(i)}");
                    break;
                }
            }

            // Wait a moment for UI to refresh
            yield return new WaitForSecondsRealtime(0.2f);
        }

        // Step 2: Click "Start Game" button
        if (SaveSlotManager.HasActiveSlot)
        {
            Debug.Log("[AutoUI] Clicking 'Start Game' button...");
            mainMenu.OnStartGame();
            Debug.Log("[AutoUI] 'Start Game' clicked");
        }
        else
        {
            Debug.LogWarning("[AutoUI] No save slots available, cannot start game");
        }

        _pendingNavigation = null;
    }

    #endregion

    #region Character Select Navigation

    public void NavigateCharacterSelect()
    {
        if (!PlayerController.IsAutoPilot) return;

        Debug.Log("[AutoUI] Starting character select navigation...");

        // Cancel any existing navigation
        if (_pendingNavigation != null)
        {
            StopCoroutine(_pendingNavigation);
        }

        _pendingNavigation = StartCoroutine(NavigateCharacterSelectCoroutine());
    }

    private IEnumerator NavigateCharacterSelectCoroutine()
    {
        yield return new WaitForSecondsRealtime(_characterSelectDelay);

        var charSelect = FindObjectOfType<CharacterSelectUI>();
        if (charSelect == null)
        {
            Debug.LogWarning("[AutoUI] CharacterSelectUI not found, skipping navigation");
            _pendingNavigation = null;
            yield break;
        }

        // Find the first unlocked character
        var characters = UnlockManager.Instance?.AllCharacters;
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("[AutoUI] No characters available");
            _pendingNavigation = null;
            yield break;
        }

        int selectedIndex = -1;
        for (int i = 0; i < characters.Count; i++)
        {
            if (UnlockManager.Instance.IsUnlocked(characters[i].id))
            {
                selectedIndex = i;
                Debug.Log($"[AutoUI] Selected character: {characters[i].characterName}");
                break;
            }
        }

        if (selectedIndex < 0)
        {
            Debug.LogWarning("[AutoUI] No unlocked characters found");
            _pendingNavigation = null;
            yield break;
        }

        // Call OnStartGame() with the selected character
        // We need to set the selected index first, then call start
        var field = typeof(CharacterSelectUI).GetField("_selectedIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(charSelect, selectedIndex);
        }

        // Call OnStartGame method
        charSelect.OnStartGame();
        Debug.Log("[AutoUI] 'Start Battle' clicked");

        _pendingNavigation = null;
    }

    #endregion

    #region Result Screen Navigation

    public void NavigateResultScreen()
    {
        if (!PlayerController.IsAutoPilot) return;

        Debug.Log("[AutoUI] Starting result screen navigation...");

        // Cancel any existing navigation
        if (_pendingNavigation != null)
        {
            StopCoroutine(_pendingNavigation);
        }

        _pendingNavigation = StartCoroutine(NavigateResultScreenCoroutine());
    }

    private IEnumerator NavigateResultScreenCoroutine()
    {
        yield return new WaitForSecondsRealtime(_resultScreenDelay);

        var resultScreen = FindObjectOfType<ResultScreen>();
        if (resultScreen == null)
        {
            Debug.LogWarning("[AutoUI] ResultScreen not found, skipping navigation");
            _pendingNavigation = null;
            yield break;
        }

        Debug.Log("[AutoUI] Clicking 'Retry' button...");
        resultScreen.Retry();
        Debug.Log("[AutoUI] 'Retry' clicked");

        _pendingNavigation = null;
    }

    #endregion

    #region Public API for External Triggers

    /// <summary>
    /// Manually trigger navigation check (e.g., after a custom UI shows up).
    /// This can be called from other UI scripts when they display.
    /// </summary>
    public void TriggerNavigationCheck()
    {
        if (PlayerController.IsAutoPilot)
        {
            CheckAndNavigate();
        }
    }

    #endregion
}
