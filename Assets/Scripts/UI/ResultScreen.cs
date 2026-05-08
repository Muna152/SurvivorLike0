using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Enhanced game over / victory result screen with detailed statistics.
/// Uses CanvasGroup so the panel stays active (Start() always runs) while being invisible.
/// </summary>
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _timeText;
    [SerializeField] private Text _killsText;
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _eliteKillsText;
    [SerializeField] private Text _healedText;
    [SerializeField] private Text _characterText;
    [SerializeField] private Text _unlockText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _menuButton;

    private CanvasGroup _canvasGroup;
    private bool _persisted;

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

    public void Show(bool victory)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        Time.timeScale = 0f;

        if (_titleText != null)
            _titleText.text = victory ? "🎉 VICTORY!" : "💀 GAME OVER";

        var gm = GameManager.Instance;
        var stats = FindObjectOfType<PlayerStats>();

        if (_timeText != null && gm != null)
        {
            int min = (int)(gm.ElapsedTime / 60f);
            int sec = (int)(gm.ElapsedTime % 60f);
            _timeText.text = $"⏱ 存活时间: {min:D2}:{sec:D2}";
        }

        if (_killsText != null && stats != null)
            _killsText.text = $"⚔ 击杀数: {stats.KillCount}";

        if (_goldText != null && stats != null)
            _goldText.text = $"💰 金币: {stats.Gold}";

        if (_levelText != null && stats != null)
            _levelText.text = $"📊 等级: Lv.{stats.Level}";

        if (_eliteKillsText != null && stats != null)
            _eliteKillsText.text = $"👹 精英击杀: {stats.EliteKillCount}";

        if (_healedText != null && stats != null)
            _healedText.text = $"💚 总治疗量: {(int)stats.TotalHealed}";

        if (_characterText != null && gm != null && gm.SelectedCharacter != null)
            _characterText.text = $"🧙 角色: {gm.SelectedCharacter.characterName}";

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

        // Persist gold and stats to active save slot (once per game session)
        if (!_persisted && stats != null && gm != null && SaveSlotManager.HasActiveSlot)
        {
            _persisted = true;
            GoldManager.AddGold(stats.Gold);
            StatsTracker.AddTotalKills(stats.KillCount);
            StatsTracker.IncrementTotalGames();
            StatsTracker.UpdateBestTime(gm.ElapsedTime);
            StatsTracker.AddTotalGoldEarned(stats.Gold);
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
        _persisted = false;
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();

        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        var selectedChar = GameManager.HasInstance ? GameManager.Instance.SelectedCharacter : null;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.PendingAutoStart = selectedChar;
            GameManager.Instance.ReturnToMenu();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        _persisted = false;
        GameEvents.ClearAll();

        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        if (GameManager.HasInstance)
        {
            GameManager.Instance.PendingAutoStart = null;
            GameManager.Instance.ReturnToMenu();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}