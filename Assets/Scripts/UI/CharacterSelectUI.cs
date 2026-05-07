using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character selection screen shown before starting a game run.
/// Displays all character cards with unlock status. Selected character
/// is passed to GameManager before the game starts.
/// All UI is built programmatically so no prefab is required.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private int _selectedIndex = 0;
    private readonly List<GameObject> _cardObjects = new List<GameObject>();
    private readonly List<Image> _cardBackgrounds = new List<Image>();
    private Transform _cardContainer;
    private Text _statsText;
    private Text _descriptionText;
    private Button _startButton;
    private GameObject _lockOverlay;
    private Text _unlockHintText;

    private IReadOnlyList<CharacterData> Characters => UnlockManager.Instance.AllCharacters;

    private void Awake()
    {
        BuildUI();
        Hide(); // Start hidden, will be shown in Start() if in Menu state
    }

    private void Start()
    {
        RefreshCards();

        // Auto-start if returning from a retry (PendingAutoStart set)
        if (GameManager.HasInstance && GameManager.Instance.PendingAutoStart != null)
        {
            var character = GameManager.Instance.PendingAutoStart;
            GameManager.Instance.PendingAutoStart = null;

            // Hide main menu first — it was shown in Awake and disabled HUD / set timeScale=0
            var mainMenu = FindObjectOfType<MainMenuUI>();
            if (mainMenu != null) mainMenu.Hide();

            GameManager.Instance.StartGame(character);
            Hide();
            return;
        }

        // Start hidden — MainMenuUI controls visibility
        Hide();
    }

    // ── Public API ──────────────────────────────────────────────

    public void Show()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
        Time.timeScale = 0f;
        RefreshCards();
    }

    public void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        Time.timeScale = 1f;
    }

    // ── Card Selection ──────────────────────────────────────────

    private void SelectCard(int index)
    {
        _selectedIndex = index;
        RefreshCardVisuals();
        RefreshDetailPanel();
    }

    private void OnStartGame()
    {
        var character = Characters[_selectedIndex];
        if (character == null) return;
        if (!UnlockManager.Instance.IsUnlocked(character.id)) return;

        GameManager.Instance.StartGame(character);
        Hide();
    }

    private void OnBackToMenu()
    {
        Hide();

        var mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu != null)
            mainMenu.Show();
    }

    // ── Refresh ─────────────────────────────────────────────────

    private void RefreshCards()
    {
        var chars = Characters;
        if (chars == null) return;

        // Ensure we have the right number of card objects
        while (_cardObjects.Count < chars.Count)
        {
            CreateCard(_cardObjects.Count);
        }

        RefreshCardVisuals();
        RefreshDetailPanel();
    }

    private void RefreshCardVisuals()
    {
        var chars = Characters;
        Color selectedColor = new Color(0.9f, 0.75f, 0.2f, 0.95f);
        Color normalColor = new Color(0.2f, 0.22f, 0.28f, 0.9f);

        for (int i = 0; i < _cardObjects.Count; i++)
        {
            if (i >= chars.Count) break;

            bool isSelected = (i == _selectedIndex);
            bool isUnlocked = UnlockManager.Instance.IsUnlocked(chars[i].id);

            // Card background highlight
            if (_cardBackgrounds.Count > i && _cardBackgrounds[i] != null)
            {
                _cardBackgrounds[i].color = isSelected ? selectedColor : normalColor;
            }

            // Portrait color (grey out locked)
            var portraitImg = _cardObjects[i].transform.Find("Portrait")?.GetComponent<Image>();
            if (portraitImg != null)
            {
                portraitImg.sprite = chars[i].portrait;
                portraitImg.color = isUnlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            // Lock overlay visibility
            var lockObj = _cardObjects[i].transform.Find("LockOverlay");
            if (lockObj != null)
            {
                lockObj.gameObject.SetActive(!isUnlocked);
            }

            // Name text
            var nameTxt = _cardObjects[i].transform.Find("NameText")?.GetComponent<Text>();
            if (nameTxt != null)
            {
                nameTxt.text = chars[i].characterName;
                nameTxt.color = isUnlocked ? Color.white : Color.gray;
            }
        }

        // Start button interactability
        if (_startButton != null && chars.Count > _selectedIndex && _selectedIndex >= 0)
        {
            _startButton.interactable = UnlockManager.Instance.IsUnlocked(chars[_selectedIndex].id);
        }
    }

    private void RefreshDetailPanel()
    {
        var chars = Characters;
        if (chars == null || _selectedIndex >= chars.Count || _selectedIndex < 0) return;

        var character = chars[_selectedIndex];
        bool isUnlocked = UnlockManager.Instance.IsUnlocked(character.id);

        // Stats text
        if (_statsText != null)
        {
            if (isUnlocked)
            {
                _statsText.text =
                    $"HP: {character.baseHP:F0}   移速: {character.moveSpeed:F1}   护甲: {character.armor}\n" +
                    $"拾取范围: {character.pickupRange:F1}   幸运: {character.luck:F0}   再生: {character.regen:F1}\n" +
                    $"伤害倍率: {character.damageMultiplier:F0%}   冷却倍率: {character.cooldownMultiplier:F0%}\n" +
                    $"范围倍率: {character.areaMultiplier:F0%}   弹数加成: +{character.projectileBonus}\n" +
                    $"起始武器: {(character.startingWeapon != null ? character.startingWeapon.weaponName : "无")}";
            }
            else
            {
                _statsText.text = "???";
            }
        }

        // Description text
        if (_descriptionText != null)
        {
            if (isUnlocked)
            {
                _descriptionText.text = character.description;
            }
            else
            {
                _descriptionText.text = "";
            }
        }

        // Lock overlay on detail panel
        if (_lockOverlay != null)
        {
            _lockOverlay.SetActive(!isUnlocked);
        }

        // Unlock hint
        if (_unlockHintText != null)
        {
            if (!isUnlocked && character.unlockCondition != null)
            {
                _unlockHintText.text = $"🔒 解锁条件: {character.unlockCondition.Description}";
            }
            else
            {
                _unlockHintText.text = "";
            }
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        // Root panel
        var rootObj = new GameObject("CharacterSelect");
        rootObj.transform.SetParent(transform, false);
        rootObj.transform.SetAsFirstSibling();
        var rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        _canvasGroup = rootObj.AddComponent<CanvasGroup>();

        // Dark background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(rootObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        // Title
        CreateLabel(rootObj.transform, "Title", "⚔ 暗夜幸存者", 36, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.9f), new Vector2(1f, 0.97f), Vector2.zero, TextAnchor.MiddleCenter);

        CreateLabel(rootObj.transform, "Subtitle", "选择你的角色", 20, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0f, 0.85f), new Vector2(1f, 0.9f), Vector2.zero, TextAnchor.MiddleCenter);

        // Card container (horizontal strip)
        var cardContainer = new GameObject("CardContainer");
        cardContainer.transform.SetParent(rootObj.transform, false);
        var cardContainerRect = cardContainer.AddComponent<RectTransform>();
        cardContainerRect.anchorMin = new Vector2(0.05f, 0.35f);
        cardContainerRect.anchorMax = new Vector2(0.95f, 0.82f);
        cardContainerRect.sizeDelta = Vector2.zero;

        var hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(8, 8, 8, 8);

        _cardContainer = cardContainer.transform;

        // Detail panel (below cards)
        var detailPanel = new GameObject("DetailPanel");
        detailPanel.transform.SetParent(rootObj.transform, false);
        var detailRect = detailPanel.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.1f, 0.08f);
        detailRect.anchorMax = new Vector2(0.9f, 0.33f);
        detailRect.sizeDelta = Vector2.zero;
        var detailImg = detailPanel.AddComponent<Image>();
        detailImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

        // Stats text inside detail panel
        var statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(detailPanel.transform, false);
        var statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.02f, 0.3f);
        statsRect.anchorMax = new Vector2(0.98f, 0.95f);
        statsRect.sizeDelta = Vector2.zero;
        _statsText = statsObj.AddComponent<Text>();
        _statsText.fontSize = 16;
        _statsText.color = new Color(0.85f, 0.85f, 0.9f);
        _statsText.alignment = TextAnchor.UpperLeft;
        _statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Description text
        var descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(detailPanel.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.02f, 0.02f);
        descRect.anchorMax = new Vector2(0.98f, 0.28f);
        descRect.sizeDelta = Vector2.zero;
        _descriptionText = descObj.AddComponent<Text>();
        _descriptionText.fontSize = 14;
        _descriptionText.color = new Color(0.7f, 0.7f, 0.8f);
        _descriptionText.alignment = TextAnchor.LowerLeft;
        _descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Lock overlay on detail panel
        _lockOverlay = new GameObject("LockOverlay");
        _lockOverlay.transform.SetParent(detailPanel.transform, false);
        var lockRect = _lockOverlay.AddComponent<RectTransform>();
        lockRect.anchorMin = Vector2.zero;
        lockRect.anchorMax = Vector2.one;
        lockRect.sizeDelta = Vector2.zero;
        var lockImg = _lockOverlay.AddComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.6f);
        _lockOverlay.SetActive(false);

        // Unlock hint text (centered in lock overlay)
        var hintObj = new GameObject("UnlockHint");
        hintObj.transform.SetParent(_lockOverlay.transform, false);
        var hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.sizeDelta = Vector2.zero;
        _unlockHintText = hintObj.AddComponent<Text>();
        _unlockHintText.fontSize = 22;
        _unlockHintText.color = new Color(1f, 0.8f, 0.3f);
        _unlockHintText.alignment = TextAnchor.MiddleCenter;
        _unlockHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Start game button
        _startButton = CreateButton(rootObj.transform, "StartBtn", "开始战斗",
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.07f));
        _startButton.onClick.AddListener(OnStartGame);

        // Back to menu button
        var backBtn = CreateButton(rootObj.transform, "BackBtn", "返回主菜单",
            new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.07f));
        var backImg = backBtn.GetComponent<Image>();
        if (backImg != null) backImg.color = new Color(0.35f, 0.35f, 0.4f, 0.9f);
        var backColors = backBtn.colors;
        backColors.highlightedColor = new Color(0.5f, 0.5f, 0.55f);
        backColors.pressedColor = new Color(0.25f, 0.25f, 0.3f);
        backBtn.colors = backColors;
        backBtn.onClick.AddListener(OnBackToMenu);
    }

    private void CreateCard(int index)
    {
        var cardObj = new GameObject($"Card_{index}");
        cardObj.transform.SetParent(_cardContainer, false);

        var cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.sizeDelta = Vector2.zero;

        var cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.2f, 0.22f, 0.28f, 0.9f);
        _cardBackgrounds.Add(cardImg);

        var cardBtn = cardObj.AddComponent<Button>();
        cardBtn.targetGraphic = cardImg;
        var btnColors = cardBtn.colors;
        btnColors.highlightedColor = new Color(0.35f, 0.35f, 0.4f);
        btnColors.pressedColor = new Color(0.15f, 0.15f, 0.2f);
        cardBtn.colors = btnColors;

        int capturedIndex = index;
        cardBtn.onClick.AddListener(() => SelectCard(capturedIndex));

        // Portrait area
        var portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(cardObj.transform, false);
        var portraitRect = portraitObj.AddComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.1f, 0.3f);
        portraitRect.anchorMax = new Vector2(0.9f, 0.9f);
        portraitRect.sizeDelta = Vector2.zero;
        var portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        portraitImg.preserveAspect = true;

        // Name text
        var nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(cardObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.05f);
        nameRect.anchorMax = new Vector2(1f, 0.25f);
        nameRect.sizeDelta = Vector2.zero;
        var nameTxt = nameObj.AddComponent<Text>();
        nameTxt.fontSize = 18;
        nameTxt.color = Color.white;
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Lock overlay
        var lockObj = new GameObject("LockOverlay");
        lockObj.transform.SetParent(cardObj.transform, false);
        var lockOverlayRect = lockObj.AddComponent<RectTransform>();
        lockOverlayRect.anchorMin = Vector2.zero;
        lockOverlayRect.anchorMax = Vector2.one;
        lockOverlayRect.sizeDelta = Vector2.zero;
        var lockOverlayImg = lockObj.AddComponent<Image>();
        lockOverlayImg.color = new Color(0f, 0f, 0f, 0.5f);
        lockObj.SetActive(false);

        // Lock icon text
        var lockTxtObj = new GameObject("LockText");
        lockTxtObj.transform.SetParent(lockObj.transform, false);
        var lockTxtRect = lockTxtObj.AddComponent<RectTransform>();
        lockTxtRect.anchorMin = Vector2.zero;
        lockTxtRect.anchorMax = Vector2.one;
        lockTxtRect.sizeDelta = Vector2.zero;
        var lockTxt = lockTxtObj.AddComponent<Text>();
        lockTxt.fontSize = 32;
        lockTxt.text = "🔒";
        lockTxt.alignment = TextAnchor.MiddleCenter;
        lockTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _cardObjects.Add(cardObj);
    }

    // ── UI Helpers (same pattern as PauseMenuController) ────────

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

    private static Button CreateButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.8f, 0.5f, 0.1f, 0.9f);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var txt = txtObj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.7f, 0.2f);
        colors.pressedColor = new Color(0.6f, 0.35f, 0.05f);
        btn.colors = colors;

        return btn;
    }
}
