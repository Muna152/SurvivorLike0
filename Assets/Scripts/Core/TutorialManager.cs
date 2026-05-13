using UnityEngine;

/// <summary>
/// Manages the tutorial system: tracks which steps are completed,
/// persists completion via PlayerPrefs, and coordinates with TutorialUI.
/// </summary>
public class TutorialManager : Singleton<TutorialManager>
{
    private const string PREF_KEY = "TutorialCompleted";

    public enum TutorialStep
    {
        None = -1,
        Move = 0,
        AutoWeapon = 1,
        CollectEXP = 2,
        LevelUp = 3,
        BossWarning = 4,
        Complete = 5
    }

    private TutorialStep _currentStep = TutorialStep.None;
    private bool _isActive;
    private float _autoWeaponTimer;
    private float _bossWarningTimer;
    private bool _expCollectedThisRun;
    private bool _leveledUpThisRun;

    /// <summary>Whether the tutorial has been fully completed (persisted).</summary>
    public bool IsTutorialCompleted => PlayerPrefs.GetInt(PREF_KEY, 0) == 1;

    /// <summary>Whether the tutorial is currently active and showing steps.</summary>
    public bool IsActive => _isActive;

    /// <summary>Current tutorial step index.</summary>
    public TutorialStep CurrentStep => _currentStep;

    protected override void Awake()
    {
        base.Awake();
        // Subscribe to game events for auto-advance
        GameEvents.OnDropCollected += OnDropCollected;
        GameEvents.OnPlayerLevelUp += OnPlayerLevelUp;
    }

    private void OnDestroy()
    {
        GameEvents.OnDropCollected -= OnDropCollected;
        GameEvents.OnPlayerLevelUp -= OnPlayerLevelUp;
    }

    // ── Public API ─────────────────────────────────────────────

    /// <summary>Start the tutorial from the first step. Only runs if not already completed.</summary>
    public void StartTutorial()
    {
        if (IsTutorialCompleted) return;

        _isActive = true;
        _currentStep = TutorialStep.Move;
        _autoWeaponTimer = 0f;
        _bossWarningTimer = 0f;
        _expCollectedThisRun = false;
        _leveledUpThisRun = false;

        ShowCurrentStep();
    }

    /// <summary>Force-start the tutorial even if previously completed (for testing).</summary>
    public void ForceStartTutorial()
    {
        PlayerPrefs.DeleteKey(PREF_KEY);
        StartTutorial();
    }

    /// <summary>Skip the tutorial entirely.</summary>
    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    /// <summary>Mark tutorial as completed and hide UI.</summary>
    public void CompleteTutorial()
    {
        _isActive = false;
        _currentStep = TutorialStep.Complete;
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();

        var tutorialUI = FindObjectOfType<TutorialUI>();
        if (tutorialUI != null)
            tutorialUI.Hide();
    }

    // ── Update (checks auto-advance conditions) ────────────────

    private void Update()
    {
        if (!_isActive) return;

        switch (_currentStep)
        {
            case TutorialStep.Move:
                // Advance when player presses WASD
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                    Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                    Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                {
                    AdvanceStep();
                }
                break;

            case TutorialStep.AutoWeapon:
                // Auto-advance after 3 seconds
                _autoWeaponTimer += Time.unscaledDeltaTime;
                if (_autoWeaponTimer >= 3f)
                    AdvanceStep();
                break;

            case TutorialStep.CollectEXP:
                // Advanced by OnDropCollected event
                break;

            case TutorialStep.LevelUp:
                // Advanced by OnPlayerLevelUp event, or after 15s timeout
                if (_leveledUpThisRun)
                {
                    _bossWarningTimer += Time.unscaledDeltaTime;
                    if (_bossWarningTimer >= 1f)
                        AdvanceStep();
                }
                else
                {
                    _bossWarningTimer += Time.unscaledDeltaTime;
                    if (_bossWarningTimer >= 15f)
                        AdvanceStep();
                }
                break;

            case TutorialStep.BossWarning:
                // Auto-advance after 3 seconds
                _bossWarningTimer += Time.unscaledDeltaTime;
                if (_bossWarningTimer >= 3f)
                    AdvanceStep();
                break;
        }
    }

    // ── Event Handlers ──────────────────────────────────────────

    private void OnDropCollected(DropBase drop)
    {
        if (!_isActive || _currentStep != TutorialStep.CollectEXP) return;

        // Check if this was an EXP drop
        if (drop != null && drop.Type == DropBase.DropType.ExpGem)
        {
            _expCollectedThisRun = true;
            AdvanceStep();
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        if (!_isActive) return;

        if (_currentStep == TutorialStep.CollectEXP && _expCollectedThisRun)
        {
            // Jump ahead — player leveled up before the collect step completed
            AdvanceStep();
        }
        else if (_currentStep == TutorialStep.LevelUp)
        {
            _leveledUpThisRun = true;
            // Will advance after a short delay in Update
        }
    }

    // ── Step Progression ────────────────────────────────────────

    private void AdvanceStep()
    {
        int next = (int)_currentStep + 1;
        _currentStep = (TutorialStep)next;

        if (_currentStep >= TutorialStep.Complete)
        {
            CompleteTutorial();
            return;
        }

        // Reset timers for new step
        _autoWeaponTimer = 0f;
        _bossWarningTimer = 0f;

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        var tutorialUI = FindObjectOfType<TutorialUI>();
        if (tutorialUI != null)
            tutorialUI.ShowStep(_currentStep);
    }
}
