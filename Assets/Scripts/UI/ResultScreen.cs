using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Game over / victory result screen.
/// Uses CanvasGroup so the panel stays active (Start() always runs) while being invisible.
/// </summary>
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _timeText;
    [SerializeField] private Text _killsText;
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _unlockText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _menuButton;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        GameEvents.OnPlayerDied += ShowGameOver;

        if (_retryButton != null)
            _retryButton.onClick.AddListener(Retry);
        if (_menuButton != null)
            _menuButton.onClick.AddListener(ReturnToMenu);

        Hide();
    }

    private void OnDestroy()
    {
        GameEvents.OnPlayerDied -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        Show(false);
    }

    public void ShowVictory()
    {
        Show(true);
    }

    private void Show(bool victory)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        Time.timeScale = 0f;

        if (_titleText != null)
            _titleText.text = victory ? "VICTORY!" : "GAME OVER";

        var gm = GameManager.Instance;
        var stats = FindObjectOfType<PlayerStats>();

        if (_timeText != null && gm != null)
        {
            int min = (int)(gm.ElapsedTime / 60f);
            int sec = (int)(gm.ElapsedTime % 60f);
            _timeText.text = $"Time: {min:D2}:{sec:D2}";
        }

        if (_killsText != null && stats != null)
            _killsText.text = $"Kills: {stats.KillCount}";

        if (_goldText != null && stats != null)
            _goldText.text = $"Gold: {stats.Gold}";

        // Check character unlocks at game end
        if (_unlockText != null)
            _unlockText.text = "";

        if (UnlockManager.HasInstance && stats != null && gm != null)
        {
            var newlyUnlocked = UnlockManager.Instance.CheckUnlocks(stats, gm.ElapsedTime);
            if (newlyUnlocked.Count > 0 && _unlockText != null)
            {
                var names = new System.Text.StringBuilder("🔓 新角色解锁: ");
                for (int i = 0; i < newlyUnlocked.Count; i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(newlyUnlocked[i].characterName);
                }
                _unlockText.text = names.ToString();
            }
        }
    }

    private void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();

        // Reset object pools
        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        // Store selected character before scene reload
        var selectedChar = GameManager.HasInstance ? GameManager.Instance.SelectedCharacter : null;

        // Reload scene (this will trigger CharacterSelectUI to show in Menu state)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // Re-apply selected character after scene load (next frame)
        // CharacterSelectUI will show in Menu state, and player can choose same or different character
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        GameEvents.ClearAll();

        // Reload scene to return to menu/character select
        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}