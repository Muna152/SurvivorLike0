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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        GameEvents.ClearAll();
        if (GameManager.HasInstance) GameManager.Instance.ReturnToMenu();
    }
}