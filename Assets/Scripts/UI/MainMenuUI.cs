using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu screen shown before character selection.
/// Displays Start Game and Quit Game buttons, plus a save management panel.
/// All UI is built programmatically so no prefab is required.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private GameObject _savePanel;
    private InputField _inputField;
    private Text _statusText;
    private Button _startButton;
    private Button _shopButton;
    private UpgradeShopUI _shopUI;

    // Per-slot row references for dynamic refresh
    private readonly Text[] _slotNameTexts = new Text[SaveSlotManager.MAX_SLOTS];
    private readonly GameObject[] _slotActiveIndicators = new GameObject[SaveSlotManager.MAX_SLOTS];
    private readonly Button[] _slotSelectButtons = new Button[SaveSlotManager.MAX_SLOTS];
    private readonly Button[] _slotDeleteButtons = new Button[SaveSlotManager.MAX_SLOTS];
    private readonly GameObject[] _slotEmptyLabels = new GameObject[SaveSlotManager.MAX_SLOTS];

    private CharacterSelectUI _characterSelectUI;
    private HUDController _hudController;
    private GameObject _hudCanvas;

    private void Awake()
    {
        _hudCanvas = GetComponentInParent<Canvas>()?.gameObject;
        BuildUI();
        UINavUtil.DisableAll(transform);

        // Only show the menu if the game is in Menu state.
        // If the game is already Playing (e.g., auto-start after restart),
        // skip Show() to avoid setting timeScale=0 and hiding the HUD.
        if (!GameManager.HasInstance || GameManager.Instance.CurrentState == GameManager.GameState.Menu)
        {
            Show();
        }
        else
        {
            // Immediately hide so it doesn't block the game
            Hide();
        }
    }

    private void Start()
    {
        _characterSelectUI = FindObjectOfType<CharacterSelectUI>();
        _hudController = FindObjectOfType<HUDController>();
        _hudCanvas = FindObjectOfType<Canvas>()?.gameObject;

        if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameManager.GameState.Menu)
        {
            Hide();
        }
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

        // Move to top of canvas render order
        if (_canvasGroup != null)
            _canvasGroup.transform.SetAsLastSibling();

        // Hide HUD elements during menu
        SetHUDEnabled(false);

        RefreshSaveSlots();
        RefreshStartButton();
    }

    public void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    // ── Button Handlers ─────────────────────────────────────────

    private void OnStartGame()
    {
        if (!SaveSlotManager.HasActiveSlot) return;

        Hide();

        // Show HUD elements for gameplay
        SetHUDEnabled(true);

        if (_characterSelectUI != null)
            _characterSelectUI.Show();
    }

    private void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnOpenShop()
    {
        if (!SaveSlotManager.HasActiveSlot) return;

        if (_shopUI == null)
        {
            _shopUI = gameObject.AddComponent<UpgradeShopUI>();
        }
        _shopUI.Show();
    }

    private void OnBackToMenu()
    {
        if (_characterSelectUI != null)
            _characterSelectUI.Hide();

        if (GameManager.HasInstance)
            GameManager.Instance.ReturnToMenu();

        Show();
    }

    // ── Save Panel ──────────────────────────────────────────────

    private void ToggleSavePanel()
    {
        if (_savePanel == null) return;
        bool show = !_savePanel.activeSelf;
        _savePanel.SetActive(show);
        if (show) RefreshSaveSlots();
    }

    private void OnCreateSlot()
    {
        if (_inputField == null) return;

        string name = _inputField.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            SetStatus("请输入存档名称");
            return;
        }

        if (SaveSlotManager.IsDuplicateName(name))
        {
            SetStatus("该名称已被使用");
            return;
        }

        if (SaveSlotManager.UsedSlotCount >= SaveSlotManager.MAX_SLOTS)
        {
            SetStatus("存档栏位已满");
            return;
        }

        int index = SaveSlotManager.CreateSlot(name);
        if (index >= 0)
        {
            _inputField.text = "";
            SetStatus($"存档 '{name}' 创建成功");
        }

        RefreshSaveSlots();
        RefreshStartButton();
    }

    private void OnSelectSlot(int index)
    {
        if (!SaveSlotManager.HasSlot(index)) return;
        SaveSlotManager.SwitchSlot(index);
        RefreshSaveSlots();
        RefreshStartButton();
    }

    private void OnDeleteSlot(int index)
    {
        if (!SaveSlotManager.HasSlot(index)) return;
        string name = SaveSlotManager.GetSlotName(index);
        SaveSlotManager.DeleteSlot(index);
        SetStatus($"存档 '{name}' 已删除");
        RefreshSaveSlots();
        RefreshStartButton();
    }

    // ── Refresh ─────────────────────────────────────────────────

    private void RefreshSaveSlots()
    {
        for (int i = 0; i < SaveSlotManager.MAX_SLOTS; i++)
        {
            bool hasSlot = SaveSlotManager.HasSlot(i);
            bool isActive = SaveSlotManager.ActiveSlotIndex == i;

            // Name text
            if (_slotNameTexts[i] != null)
            {
                _slotNameTexts[i].text = hasSlot ? SaveSlotManager.GetSlotName(i) : "";
                _slotNameTexts[i].color = isActive
                    ? new Color(1f, 0.85f, 0.3f)
                    : Color.white;
            }

            // Active indicator
            if (_slotActiveIndicators[i] != null)
            {
                _slotActiveIndicators[i].SetActive(isActive);
            }

            // Select button (only show for existing, non-active slots)
            if (_slotSelectButtons[i] != null)
            {
                _slotSelectButtons[i].gameObject.SetActive(hasSlot && !isActive);
                if (hasSlot && !isActive)
                {
                    var txt = _slotSelectButtons[i].GetComponentInChildren<Text>();
                    if (txt != null) txt.text = "选择";
                }
            }

            // Delete button (only show for existing slots)
            if (_slotDeleteButtons[i] != null)
            {
                _slotDeleteButtons[i].gameObject.SetActive(hasSlot);
            }

            // Empty label
            if (_slotEmptyLabels[i] != null)
            {
                _slotEmptyLabels[i].SetActive(!hasSlot);
            }
        }
    }

    private void RefreshStartButton()
    {
        if (_startButton != null)
        {
            _startButton.interactable = SaveSlotManager.HasActiveSlot;
        }
        if (_shopButton != null)
        {
            _shopButton.interactable = SaveSlotManager.HasActiveSlot;
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        // Root panel
        var rootObj = new GameObject("MainMenu");
        rootObj.transform.SetParent(transform, false);
        rootObj.transform.SetAsLastSibling();
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
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.97f);

        // Title
        CreateLabel(rootObj.transform, "Title", "⚔ 暗夜幸存者", 42, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.78f), new Vector2(1f, 0.88f), Vector2.zero, TextAnchor.MiddleCenter);

        // Subtitle
        CreateLabel(rootObj.transform, "Subtitle", "Survive the Night", 18, new Color(0.6f, 0.6f, 0.65f),
            new Vector2(0f, 0.72f), new Vector2(1f, 0.77f), Vector2.zero, TextAnchor.MiddleCenter);

        // Active save slot indicator
        var activeSlotLabel = CreateLabel(rootObj.transform, "ActiveSlotLabel", "", 16, new Color(0.7f, 0.7f, 0.75f),
            new Vector2(0f, 0.66f), new Vector2(1f, 0.71f), Vector2.zero, TextAnchor.MiddleCenter);

        // Start Game button
        _startButton = CreateMenuButton(rootObj.transform, "StartBtn", "⚔ 开始游戏",
            new Vector2(0.3f, 0.42f), new Vector2(0.7f, 0.52f),
            new Color(0.8f, 0.5f, 0.1f, 0.9f));
        _startButton.onClick.AddListener(OnStartGame);

        // Shop button
        _shopButton = CreateMenuButton(rootObj.transform, "ShopBtn", "🛒 商店",
            new Vector2(0.3f, 0.30f), new Vector2(0.7f, 0.40f),
            new Color(0.2f, 0.55f, 0.3f, 0.9f));
        _shopButton.onClick.AddListener(OnOpenShop);

        // Quit Game button
        var quitBtn = CreateMenuButton(rootObj.transform, "QuitBtn", "✕ 退出游戏",
            new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.28f),
            new Color(0.5f, 0.15f, 0.15f, 0.9f));
        quitBtn.onClick.AddListener(OnQuitGame);

        // Save Management button (top-left)
        var saveBtn = CreateMenuButton(rootObj.transform, "SaveBtn", "📁 存档管理",
            new Vector2(0.02f, 0.90f), new Vector2(0.22f, 0.96f),
            new Color(0.3f, 0.3f, 0.38f, 0.9f));
        saveBtn.onClick.AddListener(ToggleSavePanel);

        // Build save management panel (initially hidden)
        BuildSavePanel(rootObj.transform);
    }

    private void BuildSavePanel(Transform parent)
    {
        _savePanel = new GameObject("SavePanel");
        _savePanel.transform.SetParent(parent, false);
        var panelRect = _savePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.1f);
        panelRect.anchorMax = new Vector2(0.75f, 0.88f);
        panelRect.sizeDelta = Vector2.zero;

        var panelImg = _savePanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.14f, 0.97f);

        // Title
        CreateLabel(_savePanel.transform, "PanelTitle", "📁 存档管理", 28, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.92f), new Vector2(1f, 0.98f), Vector2.zero, TextAnchor.MiddleCenter);

        // Slot rows
        for (int i = 0; i < SaveSlotManager.MAX_SLOTS; i++)
        {
            BuildSlotRow(_savePanel.transform, i);
        }

        // Input area (create new slot)
        var inputContainer = new GameObject("InputContainer");
        inputContainer.transform.SetParent(_savePanel.transform, false);
        var inputRect = inputContainer.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.05f, 0.08f);
        inputRect.anchorMax = new Vector2(0.95f, 0.20f);
        inputRect.sizeDelta = Vector2.zero;

        var inputBg = inputContainer.AddComponent<Image>();
        inputBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Input field
        var inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(inputContainer.transform, false);
        var ifRect = inputFieldObj.AddComponent<RectTransform>();
        ifRect.anchorMin = new Vector2(0.02f, 0.15f);
        ifRect.anchorMax = new Vector2(0.68f, 0.85f);
        ifRect.sizeDelta = Vector2.zero;

        var ifBg = inputFieldObj.AddComponent<Image>();
        ifBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var ifPlaceholder = new GameObject("Placeholder");
        ifPlaceholder.transform.SetParent(inputFieldObj.transform, false);
        var phRect = ifPlaceholder.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.sizeDelta = Vector2.zero;
        var phText = ifPlaceholder.AddComponent<Text>();
        phText.text = "输入存档名称...";
        phText.fontSize = 16;
        phText.color = new Color(0.5f, 0.5f, 0.55f);
        phText.alignment = TextAnchor.MiddleLeft;
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var ifTextObj = new GameObject("Text");
        ifTextObj.transform.SetParent(inputFieldObj.transform, false);
        var iftRect = ifTextObj.AddComponent<RectTransform>();
        iftRect.anchorMin = Vector2.zero;
        iftRect.anchorMax = Vector2.one;
        iftRect.sizeDelta = Vector2.zero;
        var ifText = ifTextObj.AddComponent<Text>();
        ifText.text = "";
        ifText.fontSize = 16;
        ifText.color = Color.white;
        ifText.alignment = TextAnchor.MiddleLeft;
        ifText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _inputField = inputFieldObj.AddComponent<InputField>();
        _inputField.targetGraphic = ifBg;
        _inputField.textComponent = ifText;
        _inputField.placeholder = phText;
        _inputField.text = "";
        _inputField.characterLimit = 20;

        // Create button
        var createBtn = CreateSmallButton(inputContainer.transform, "创建",
            new Vector2(0.72f, 0.15f), new Vector2(0.98f, 0.85f));
        createBtn.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.2f, 0.9f);
        var createColors = createBtn.colors;
        createColors.highlightedColor = new Color(0.3f, 0.85f, 0.3f);
        createColors.pressedColor = new Color(0.15f, 0.5f, 0.15f);
        createBtn.colors = createColors;
        createBtn.onClick.AddListener(OnCreateSlot);

        // Status text
        _statusText = CreateLabel(_savePanel.transform, "StatusText", "", 14, new Color(0.8f, 0.8f, 0.85f),
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.08f), Vector2.zero, TextAnchor.MiddleCenter);

        // Close button
        var closeBtn = CreateSmallButton(_savePanel.transform, "关闭",
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.08f));
        closeBtn.onClick.AddListener(() => _savePanel.SetActive(false));

        _savePanel.SetActive(false);
    }

    private void BuildSlotRow(Transform parent, int index)
    {
        // Calculate vertical position (rows from top)
        float topY = 0.88f;
        float rowHeight = 0.22f;
        float gap = 0.02f;
        float yMax = topY - index * (rowHeight + gap);
        float yMin = yMax - rowHeight;

        var rowObj = new GameObject($"SlotRow_{index}");
        rowObj.transform.SetParent(parent, false);
        var rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.05f, yMin);
        rowRect.anchorMax = new Vector2(0.95f, yMax);
        rowRect.sizeDelta = Vector2.zero;

        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Slot index label
        CreateLabel(rowObj.transform, "SlotIndex", $"存档 {index + 1}", 16, new Color(0.6f, 0.6f, 0.65f),
            new Vector2(0.02f, 0.1f), new Vector2(0.2f, 0.9f), Vector2.zero, TextAnchor.MiddleLeft);

        // Active indicator (small dot/text)
        var activeObj = new GameObject("ActiveIndicator");
        activeObj.transform.SetParent(rowObj.transform, false);
        var activeRect = activeObj.AddComponent<RectTransform>();
        activeRect.anchorMin = new Vector2(0.2f, 0.2f);
        activeRect.anchorMax = new Vector2(0.28f, 0.8f);
        activeRect.sizeDelta = Vector2.zero;
        var activeImg = activeObj.AddComponent<Image>();
        activeImg.color = new Color(0.2f, 0.85f, 0.2f, 1f);
        _slotActiveIndicators[index] = activeObj;

        // Slot name text
        var nameObj = new GameObject("SlotName");
        nameObj.transform.SetParent(rowObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.28f, 0.1f);
        nameRect.anchorMax = new Vector2(0.6f, 0.9f);
        nameRect.sizeDelta = Vector2.zero;
        var nameText = nameObj.AddComponent<Text>();
        nameText.fontSize = 18;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _slotNameTexts[index] = nameText;

        // Empty label (shown when slot doesn't exist)
        var emptyObj = new GameObject("EmptyLabel");
        emptyObj.transform.SetParent(rowObj.transform, false);
        var emptyRect = emptyObj.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0.28f, 0.1f);
        emptyRect.anchorMax = new Vector2(0.6f, 0.9f);
        emptyRect.sizeDelta = Vector2.zero;
        var emptyText = emptyObj.AddComponent<Text>();
        emptyText.text = "— 空栏位 —";
        emptyText.fontSize = 16;
        emptyText.color = new Color(0.4f, 0.4f, 0.45f);
        emptyText.alignment = TextAnchor.MiddleLeft;
        emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _slotEmptyLabels[index] = emptyObj;

        // Select button
        var selectBtn = CreateSmallButton(rowObj.transform, "选择",
            new Vector2(0.62f, 0.15f), new Vector2(0.78f, 0.85f));
        selectBtn.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 0.9f);
        int capturedIndex = index;
        selectBtn.onClick.AddListener(() => OnSelectSlot(capturedIndex));
        _slotSelectButtons[index] = selectBtn;

        // Delete button
        var deleteBtn = CreateSmallButton(rowObj.transform, "删除",
            new Vector2(0.82f, 0.15f), new Vector2(0.98f, 0.85f));
        deleteBtn.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
        var delColors = deleteBtn.colors;
        delColors.highlightedColor = new Color(0.9f, 0.3f, 0.3f);
        delColors.pressedColor = new Color(0.5f, 0.1f, 0.1f);
        deleteBtn.colors = delColors;
        deleteBtn.onClick.AddListener(() => OnDeleteSlot(capturedIndex));
        _slotDeleteButtons[index] = deleteBtn;
    }

    // ── UI Helpers ───────────────────────────────────────────────

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

    private static Button CreateMenuButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;

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
        txt.fontSize = 26;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.7f, 0.2f);
        colors.pressedColor = new Color(0.6f, 0.35f, 0.05f);
        btn.colors = colors;

        return btn;
    }

    private static Button CreateSmallButton(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject($"Btn_{text}");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.38f, 0.9f);

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
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.55f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
        btn.colors = colors;

        return btn;
    }

    // ── HUD Visibility ──────────────────────────────────────────

    public void SetHUDEnabled(bool enabled)
    {
        if (_hudCanvas == null) return;

        // Toggle visibility of HUD children except our own panels and other overlay UIs
        for (int i = 0; i < _hudCanvas.transform.childCount; i++)
        {
            var child = _hudCanvas.transform.GetChild(i);
            var name = child.name;

            // Skip panels that manage their own visibility
            if (name == "MainMenu" || name == "CharacterSelectUI" || name == "PauseMenu"
                || name == "ResultScreen" || name == "UpgradePanel") continue;

            child.gameObject.SetActive(enabled);
        }
    }
}
