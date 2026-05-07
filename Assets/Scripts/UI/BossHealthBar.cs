using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BOSS health bar UI: shown at the top of the screen when a boss is active.
/// Programmatically built, event-driven (subscribes to GameEvents).
/// Attached to the same Canvas as HUDController.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class BossHealthBar : MonoBehaviour
{
    private GameObject _panel;
    private Slider _hpSlider;
    private Text _bossNameText;
    private Text _hpText;
    private Image _fillImage;

    private BossEnemy _currentBoss;
    private System.Action<BossEnemy> _onBossSpawnedHandler;
    private System.Action<BossEnemy> _onBossDiedHandler;
    private System.Action<BossEnemy> _onBossHealthChangedHandler;

    private void Awake()
    {
        BuildUI();
        _panel.SetActive(false);
    }

    private void OnEnable()
    {
        _onBossSpawnedHandler = OnBossSpawned;
        _onBossDiedHandler = OnBossDied;
        _onBossHealthChangedHandler = OnBossHealthChanged;

        GameEvents.OnBossSpawned += _onBossSpawnedHandler;
        GameEvents.OnBossDied += _onBossDiedHandler;
        GameEvents.OnBossHealthChanged += _onBossHealthChangedHandler;
    }

    private void OnDisable()
    {
        GameEvents.OnBossSpawned -= _onBossSpawnedHandler;
        GameEvents.OnBossDied -= _onBossDiedHandler;
        GameEvents.OnBossHealthChanged -= _onBossHealthChangedHandler;
    }

    private void BuildUI()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        // Root panel — anchored top center
        _panel = new GameObject("BossHealthBarPanel");
        _panel.transform.SetParent(transform, false);

        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.9f);
        panelRect.anchorMax = new Vector2(0.8f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dark semi-transparent background
        var bgImage = _panel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Vertical layout
        var vGroup = _panel.AddComponent<VerticalLayoutGroup>();
        vGroup.padding = new RectOffset(10, 10, 5, 5);
        vGroup.spacing = 2;
        vGroup.childAlignment = TextAnchor.MiddleCenter;
        vGroup.childControlWidth = true;
        vGroup.childControlHeight = true;

        // Boss name text
        var nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(_panel.transform, false);
        _bossNameText = nameObj.AddComponent<Text>();
        _bossNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _bossNameText.fontSize = 18;
        _bossNameText.fontStyle = FontStyle.Bold;
        _bossNameText.color = Color.white;
        _bossNameText.alignment = TextAnchor.MiddleCenter;
        _bossNameText.text = "BOSS";

        // HP bar container
        var barObj = new GameObject("HPBar");
        barObj.transform.SetParent(_panel.transform, false);

        var barRect = barObj.AddComponent<RectTransform>();
        _hpSlider = barObj.AddComponent<Slider>();
        _hpSlider.minValue = 0f;
        _hpSlider.maxValue = 1f;
        _hpSlider.value = 1f;
        _hpSlider.interactable = false;

        // Slider background
        var sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(barObj.transform, false);
        var bgRect = sliderBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        _hpSlider.targetGraphic = bgImg;

        // Fill area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(barObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.color = Color.red;
        _hpSlider.fillRect = fillRect;

        // Handle area (hide handle)
        _hpSlider.handleRect = null;

        // HP text (overlay on bar)
        var hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(barObj.transform, false);
        var hpTextRect = hpTextObj.AddComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;
        _hpText = hpTextObj.AddComponent<Text>();
        _hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _hpText.fontSize = 14;
        _hpText.color = Color.white;
        _hpText.alignment = TextAnchor.MiddleCenter;
        _hpText.text = "";
    }

    private void OnBossSpawned(BossEnemy boss)
    {
        _currentBoss = boss;
        _bossNameText.text = boss.BossName;
        _hpSlider.value = 1f;
        _panel.SetActive(true);
        UpdateHPDisplay();
    }

    private void OnBossDied(BossEnemy boss)
    {
        if (_currentBoss == boss)
        {
            _currentBoss = null;
            _panel.SetActive(false);
        }
    }

    private void OnBossHealthChanged(BossEnemy boss)
    {
        if (_currentBoss == boss)
        {
            UpdateHPDisplay();
        }
    }

    private void UpdateHPDisplay()
    {
        if (_currentBoss == null) return;

        float percent = _currentBoss.HealthPercent;
        _hpSlider.value = Mathf.Clamp01(percent);

        // Color transitions: green → yellow → red
        if (percent > 0.5f)
            _fillImage.color = Color.Lerp(Color.yellow, Color.green, (percent - 0.5f) * 2f);
        else
            _fillImage.color = Color.Lerp(Color.red, Color.yellow, percent * 2f);

        // HP text
        int hp = Mathf.Max(0, (int)_currentBoss.CurrentHP);
        int maxHp = (int)_currentBoss.MaxHP;
        _hpText.text = $"{hp} / {maxHp}";

        // For unkillable bosses, show "∞" indicator
        if (_currentBoss.IsUnkillable)
        {
            _bossNameText.color = new Color(0.6f, 0f, 0.8f); // Purple for Death
        }
    }
}
