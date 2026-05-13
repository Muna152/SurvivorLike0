using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial overlay UI: shows step-by-step instructions for new players.
/// Built programmatically (no prefab required), uses CanvasGroup for fade.
/// Controlled by TutorialManager; not instantiated directly.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Text _stepTitleText;
    private Text _stepDescText;
    private Text _hintText;
    private Button _skipButton;
    private GameObject _panelRoot;

    // Step content data
    private static readonly string[] StepTitles =
    {
        "🎮 移动",
        "⚔️ 武器",
        "💎 经验",
        "⬆️ 升级",
        "👹 Boss"
    };

    private static readonly string[] StepDescriptions =
    {
        "使用 WASD 或方向键移动角色",
        "武器会自动攻击附近的敌人",
        "击败敌人收集经验宝石，积累升级",
        "升级时选择强化，让你的角色更强大",
        "注意！每10/20/30分钟会出现Boss！"
    };

    private static readonly string[] StepHints =
    {
        "试试移动 →",
        "等待自动攻击...",
        "收集掉落的经验宝石",
        "升级时选择一项强化",
        "做好准备..."
    };

    private void Awake()
    {
        BuildUI();
        HideImmediate();
    }

    // ── Public API ─────────────────────────────────────────────

    /// <summary>Show the UI for the given tutorial step with fade-in.</summary>
    public void ShowStep(TutorialManager.TutorialStep step)
    {
        int idx = (int)step;
        if (idx < 0 || idx >= StepTitles.Length) return;

        if (_stepTitleText != null) _stepTitleText.text = StepTitles[idx];
        if (_stepDescText != null) _stepDescText.text = StepDescriptions[idx];
        if (_hintText != null) _hintText.text = StepHints[idx];

        // Show step indicator dots
        UpdateStepIndicator(idx);

        // Fade in
        if (_panelRoot != null) _panelRoot.SetActive(true);
        FadeIn();
    }

    /// <summary>Hide with fade-out.</summary>
    public void Hide()
    {
        FadeOut();
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        // Root panel (bottom-center of screen, non-blocking)
        _panelRoot = new GameObject("TutorialUI");
        _panelRoot.transform.SetParent(transform, false);
        _panelRoot.transform.SetAsLastSibling();

        var rootRect = _panelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.15f, 0.02f);
        rootRect.anchorMax = new Vector2(0.85f, 0.18f);
        rootRect.sizeDelta = Vector2.zero;

        _canvasGroup = _panelRoot.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false; // Non-blocking — player can still interact
        _canvasGroup.interactable = false;

        // Semi-transparent dark background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_panelRoot.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);

        // Rounded border effect
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(_panelRoot.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(1f, 0.85f, 0.3f, 0.4f);

        // Step indicator (dots at top)
        var indicatorObj = new GameObject("StepIndicator");
        indicatorObj.transform.SetParent(_panelRoot.transform, false);
        var indicatorRect = indicatorObj.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0f, 0.75f);
        indicatorRect.anchorMax = new Vector2(1f, 0.95f);
        indicatorRect.sizeDelta = Vector2.zero;

        // Create dot objects for each step
        for (int i = 0; i < StepTitles.Length; i++)
        {
            var dotObj = new GameObject($"Dot_{i}");
            dotObj.transform.SetParent(indicatorObj.transform, false);
            var dotRect = dotObj.AddComponent<RectTransform>();
            float dotSize = 12f;
            float spacing = 30f;
            float startX = -(StepTitles.Length - 1) * spacing / 2f;
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
            dotRect.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            var dotImg = dotObj.AddComponent<Image>();
            dotImg.color = i == 0 ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.4f, 0.45f);
        }

        // Title text
        _stepTitleText = CreateLabel(_panelRoot.transform, "Title", "", 22,
            new Color(1f, 0.85f, 0.3f),
            new Vector2(0.05f, 0.40f), new Vector2(0.75f, 0.75f),
            Vector2.zero, TextAnchor.MiddleLeft);

        // Skip button (top-right)
        var skipObj = new GameObject("SkipBtn");
        skipObj.transform.SetParent(_panelRoot.transform, false);
        var skipRect = skipObj.AddComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.82f, 0.50f);
        skipRect.anchorMax = new Vector2(0.97f, 0.90f);
        skipRect.sizeDelta = Vector2.zero;
        var skipImg = skipObj.AddComponent<Image>();
        skipImg.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        _skipButton = skipObj.AddComponent<Button>();
        _skipButton.targetGraphic = skipImg;
        var skipTxtObj = new GameObject("Text");
        skipTxtObj.transform.SetParent(skipObj.transform, false);
        var skipTxtRect = skipTxtObj.AddComponent<RectTransform>();
        skipTxtRect.anchorMin = Vector2.zero;
        skipTxtRect.anchorMax = Vector2.one;
        skipTxtRect.sizeDelta = Vector2.zero;
        var skipTxt = skipTxtObj.AddComponent<Text>();
        skipTxt.text = "跳过";
        skipTxt.fontSize = 14;
        skipTxt.color = new Color(0.7f, 0.7f, 0.75f);
        skipTxt.alignment = TextAnchor.MiddleCenter;
        skipTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _skipButton.onClick.AddListener(OnSkipClicked);
        // Make skip button interactable even though canvasGroup.interactable is false
        // by using a separate CanvasGroup or by enabling interactable during fade
        _skipButton.interactable = true;

        // Description text
        _stepDescText = CreateLabel(_panelRoot.transform, "Desc", "", 18,
            new Color(0.9f, 0.9f, 0.95f),
            new Vector2(0.05f, 0.10f), new Vector2(0.75f, 0.40f),
            Vector2.zero, TextAnchor.MiddleLeft);

        // Hint text (subtle, bottom-right)
        _hintText = CreateLabel(_panelRoot.transform, "Hint", "", 14,
            new Color(0.6f, 0.6f, 0.65f),
            new Vector2(0.50f, 0.02f), new Vector2(0.97f, 0.15f),
            Vector2.zero, TextAnchor.MiddleRight);

        _panelRoot.SetActive(false);
    }

    private void UpdateStepIndicator(int activeIndex)
    {
        var indicator = _panelRoot.transform.Find("StepIndicator");
        if (indicator == null) return;

        for (int i = 0; i < indicator.childCount; i++)
        {
            var dot = indicator.GetChild(i);
            var img = dot.GetComponent<Image>();
            if (img != null)
            {
                img.color = i == activeIndex
                    ? new Color(1f, 0.85f, 0.3f)
                    : (i < activeIndex ? new Color(0.6f, 0.6f, 0.65f) : new Color(0.3f, 0.3f, 0.35f));
            }
        }
    }

    // ── Fade Animation ──────────────────────────────────────────

    private bool _fadingIn;
    private bool _fadingOut;
    private float _fadeAlpha;
    private const float FADE_SPEED = 4f; // Alpha per second

    private void Update()
    {
        if (_fadingIn)
        {
            _fadeAlpha += FADE_SPEED * Time.unscaledDeltaTime;
            if (_fadeAlpha >= 1f)
            {
                _fadeAlpha = 1f;
                _fadingIn = false;
            }
            ApplyFadeAlpha();
        }
        else if (_fadingOut)
        {
            _fadeAlpha -= FADE_SPEED * Time.unscaledDeltaTime;
            if (_fadeAlpha <= 0f)
            {
                _fadeAlpha = 0f;
                _fadingOut = false;
                if (_panelRoot != null) _panelRoot.SetActive(false);
            }
            ApplyFadeAlpha();
        }
    }

    private void FadeIn()
    {
        _fadingIn = true;
        _fadingOut = false;
        _fadeAlpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;
    }

    private void FadeOut()
    {
        _fadingOut = true;
        _fadingIn = false;
        _fadeAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
    }

    private void ApplyFadeAlpha()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = _fadeAlpha;
    }

    private void HideImmediate()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_panelRoot != null) _panelRoot.SetActive(false);
        _fadingIn = false;
        _fadingOut = false;
    }

    // ── Button Handlers ────────────────────────────────────────

    private void OnSkipClicked()
    {
        if (TutorialManager.HasInstance)
            TutorialManager.Instance.SkipTutorial();
    }

    // ── UI Helper ──────────────────────────────────────────────

    private static Text CreateLabel(Transform parent, string name, string text, int fontSize,
        Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, TextAnchor alignment)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(offset.x, 0);
        rect.offsetMax = new Vector2(-offset.x, 0);
        var txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return txt;
    }
}
