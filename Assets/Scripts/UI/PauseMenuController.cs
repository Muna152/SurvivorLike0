using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause menu with debug panel.
/// ESC to toggle pause. Debug panel allows adding/removing weapons and passives,
/// and adjusting their levels.
/// All UI is built programmatically so no prefab is required.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private PlayerWeaponManager _weaponManager;
    private PlayerStats _playerStats;

    // All available data assets (loaded at runtime)
    private List<WeaponData> _allWeapons = new List<WeaponData>();
    private List<PassiveData> _allPassives = new List<PassiveData>();

    // UI references for live-refresh
    private Transform _weaponListContent;
    private Transform _passiveListContent;
    private Text _weaponCountText;

    private void Awake()
    {
        BuildUI();
        UINavUtil.DisableAll(transform);
        Hide();
    }

    private void Start()
    {
        _weaponManager = FindObjectOfType<PlayerWeaponManager>();
        _playerStats = FindObjectOfType<PlayerStats>();
        LoadAllDataAssets();
    }

    // ── Input ──────────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.IsPaused)
                Resume();
            else if (GameManager.Instance.IsPlaying)
                Pause();
        }
    }

    // ── Pause / Resume ─────────────────────────────────────────

    private void Pause()
    {
        GameManager.Instance.PauseGame();
        RefreshLists();
        Show();
    }

    private void Resume()
    {
        GameManager.Instance.ResumeGame();
        Hide();
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();

        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        // Save selected character for auto-start after scene reload
        var selectedChar = GameManager.HasInstance ? GameManager.Instance.SelectedCharacter : null;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.PendingAutoStart = selectedChar;
            GameManager.Instance.ReturnToMenu();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GiveUp()
    {
        Hide();

        if (GameManager.HasInstance)
            GameManager.Instance.EndGame(false);

        var resultScreen = FindObjectOfType<ResultScreen>();
        if (resultScreen != null)
            resultScreen.Show(false);
    }

    private void ShowCodex()
    {
        var codex = FindObjectOfType<CodexUI>();
        if (codex != null)
            codex.Show();
    }

    // ── Data Loading ────────────────────────────────────────────

    private void LoadAllDataAssets()
    {
        _allWeapons.Clear();
        _allPassives.Clear();

        var weaponDb = WeaponDatabase.Instance;
        if (weaponDb != null && weaponDb.weapons != null)
            _allWeapons.AddRange(weaponDb.weapons);

        var passiveDb = PassiveDatabase.Instance;
        if (passiveDb != null && passiveDb.passives != null)
            _allPassives.AddRange(passiveDb.passives);
    }

    // ── Show / Hide ────────────────────────────────────────────

    private void Show()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
    }

    private void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        // Root panel (full-screen overlay)
        var rootObj = new GameObject("PauseMenu");
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
        bgImg.color = new Color(0f, 0f, 0f, 0.8f);

        // Content container (centered panel)
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(rootObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.05f);
        panelRect.anchorMax = new Vector2(0.9f, 0.95f);
        panelRect.sizeDelta = Vector2.zero;
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        // Title
        CreateLabel(panelObj.transform, "Title", "⏸ 暋停", 32, Color.white,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -50),
            TextAnchor.UpperCenter);

        // Resume button (full-width, top row — most prominent)
        var resumeBtn = CreateButton(panelObj.transform, "ResumeBtn", "继续游戏",
            new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.97f));
        resumeBtn.onClick.AddListener(Resume);

        // Restart button
        var restartBtn = CreateButton(panelObj.transform, "RestartBtn", "重新开始",
            new Vector2(0.05f, 0.86f), new Vector2(0.30f, 0.91f));
        var restartImg = restartBtn.GetComponent<Image>();
        if (restartImg != null) restartImg.color = new Color(0.6f, 0.4f, 0.1f, 0.9f);
        var restartColors = restartBtn.colors;
        restartColors.highlightedColor = new Color(0.8f, 0.55f, 0.15f);
        restartColors.pressedColor = new Color(0.5f, 0.3f, 0.05f);
        restartBtn.colors = restartColors;
        restartBtn.onClick.AddListener(Restart);

        // Codex button
        var codexBtn = CreateButton(panelObj.transform, "CodexBtn", "📖 图鉴",
            new Vector2(0.35f, 0.86f), new Vector2(0.65f, 0.91f));
        var codexImg = codexBtn.GetComponent<Image>();
        if (codexImg != null) codexImg.color = new Color(0.3f, 0.3f, 0.55f, 0.9f);
        var codexColors = codexBtn.colors;
        codexColors.highlightedColor = new Color(0.4f, 0.4f, 0.7f);
        codexColors.pressedColor = new Color(0.2f, 0.2f, 0.4f);
        codexBtn.colors = codexColors;
        codexBtn.onClick.AddListener(ShowCodex);

        // Give up button
        var giveUpBtn = CreateButton(panelObj.transform, "GiveUpBtn", "放弃",
            new Vector2(0.70f, 0.86f), new Vector2(0.95f, 0.91f));
        var giveUpImg = giveUpBtn.GetComponent<Image>();
        if (giveUpImg != null) giveUpImg.color = new Color(0.55f, 0.2f, 0.1f, 0.9f);
        var giveUpColors = giveUpBtn.colors;
        giveUpColors.highlightedColor = new Color(0.75f, 0.3f, 0.15f);
        giveUpColors.pressedColor = new Color(0.4f, 0.15f, 0.05f);
        giveUpBtn.colors = giveUpColors;
        giveUpBtn.onClick.AddListener(GiveUp);

        // Weapon section header + count
        CreateLabel(panelObj.transform, "WeaponHeader", "── 武器 ──", 24, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.82f), new Vector2(0.5f, 0.87f), Vector2.zero, TextAnchor.MiddleCenter);
        _weaponCountText = CreateLabel(panelObj.transform, "WeaponCount", "", 16, Color.gray,
            new Vector2(0.5f, 0.82f), new Vector2(1f, 0.87f), Vector2.zero, TextAnchor.MiddleRight);

        // Weapon scroll area
        var weaponScroll = CreateScrollView(panelObj.transform, "WeaponScroll",
            new Vector2(0.02f, 0.44f), new Vector2(0.98f, 0.81f));
        _weaponListContent = weaponScroll;

        // Passive section header
        CreateLabel(panelObj.transform, "PassiveHeader", "── 增益 ──", 24, new Color(0.3f, 0.85f, 1f),
            new Vector2(0f, 0.40f), new Vector2(1f, 0.44f), Vector2.zero, TextAnchor.MiddleCenter);

        // Passive scroll area
        var passiveScroll = CreateScrollView(panelObj.transform, "PassiveScroll",
            new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.39f));
        _passiveListContent = passiveScroll;
    }

    // ── List Refresh ────────────────────────────────────────────

    private void RefreshLists()
    {
        RefreshWeaponList();
        RefreshPassiveList();
    }

    private void RefreshWeaponList()
    {
        ClearChildren(_weaponListContent);

        if (_weaponManager == null) return;

        var equipped = _weaponManager.EquippedWeapons;
        _weaponCountText.text = $"({equipped.Count}/6)";

        // Equipped weapons
        foreach (var weapon in equipped)
        {
            CreateWeaponRow(_weaponListContent, weapon.Data, weapon);
        }

        // Available weapons (not yet equipped, not evolution-only)
        foreach (var data in _allWeapons)
        {
            if (data.isEvolutionOnly) continue;
            if (_weaponManager.HasWeapon(data)) continue;
            CreateWeaponRow(_weaponListContent, data, null);
        }
    }

    private void RefreshPassiveList()
    {
        ClearChildren(_passiveListContent);

        if (_playerStats == null) return;

        foreach (var passive in _allPassives)
        {
            int level = _playerStats.GetPassiveLevel(passive);
            CreatePassiveRow(_passiveListContent, passive, level);
        }
    }

    // ── Weapon Row ──────────────────────────────────────────────

    private void CreateWeaponRow(Transform parent, WeaponData data, WeaponBase equipped)
    {
        bool isEquipped = equipped != null;
        int level = isEquipped ? equipped.CurrentLevel : 0;
        int maxLevel = data.maxLevel;

        var row = CreateRow(parent, $"Weapon_{data.weaponName}");

        // Name
        CreateLabel(row, "Name", data.weaponName, 16,
            isEquipped ? Color.white : new Color(0.6f, 0.6f, 0.6f),
            new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Vector2(8, 0), TextAnchor.MiddleLeft);

        // Level display
        var lvlLabel = CreateLabel(row, "Level", isEquipped ? $"Lv.{level}/{maxLevel}" : "--",
            14, isEquipped ? Color.yellow : Color.gray,
            new Vector2(0.3f, 0f), new Vector2(0.48f, 1f), Vector2.zero, TextAnchor.MiddleCenter);

        // - Level button
        var minusBtn = CreateSmallButton(row, "-", new Vector2(0.50f, 0.15f), new Vector2(0.56f, 0.85f));
        minusBtn.interactable = isEquipped && level > 1;
        minusBtn.onClick.AddListener(() =>
        {
            if (equipped != null && equipped.CurrentLevel > 1)
            {
                equipped.SetLevel(equipped.CurrentLevel - 1);
                RefreshWeaponList();
            }
        });

        // + Level button
        var plusBtn = CreateSmallButton(row, "+", new Vector2(0.58f, 0.15f), new Vector2(0.64f, 0.85f));
        plusBtn.interactable = isEquipped && level < maxLevel;
        plusBtn.onClick.AddListener(() =>
        {
            if (equipped != null && equipped.CurrentLevel < equipped.MaxLevel)
            {
                equipped.Upgrade();
                RefreshWeaponList();
            }
        });

        // Max button
        var maxBtn = CreateSmallButton(row, "MAX", new Vector2(0.66f, 0.15f), new Vector2(0.74f, 0.85f));
        maxBtn.interactable = isEquipped && level < maxLevel;
        maxBtn.onClick.AddListener(() =>
        {
            if (equipped != null)
            {
                equipped.SetLevel(equipped.MaxLevel);
                RefreshWeaponList();
            }
        });

        // Add / Remove button
        var toggleBtn = CreateSmallButton(row,
            isEquipped ? "移除" : "添加",
            new Vector2(0.78f, 0.1f), new Vector2(0.95f, 0.9f));
        var toggleImg = toggleBtn.GetComponent<Image>();
        if (toggleImg != null)
            toggleImg.color = isEquipped ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.2f);

        toggleBtn.onClick.AddListener(() =>
        {
            if (isEquipped)
            {
                _weaponManager.RemoveWeapon(data);
            }
            else
            {
                _weaponManager.EquipWeapon(data);
            }
            RefreshLists();
        });
    }

    // ── Passive Row ─────────────────────────────────────────────

    private void CreatePassiveRow(Transform parent, PassiveData passive, int currentLevel)
    {
        int maxLevel = passive.maxLevel;
        bool owned = currentLevel > 0;

        var row = CreateRow(parent, $"Passive_{passive.passiveName}");

        // Name
        CreateLabel(row, "Name", passive.passiveName, 16,
            owned ? Color.white : new Color(0.6f, 0.6f, 0.6f),
            new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Vector2(8, 0), TextAnchor.MiddleLeft);

        // Stat info
        CreateLabel(row, "Stat", $"({passive.affectedStat})", 11,
            new Color(0.7f, 0.7f, 0.7f),
            new Vector2(0.3f, 0f), new Vector2(0.48f, 1f), Vector2.zero, TextAnchor.MiddleCenter);

        // Level display
        CreateLabel(row, "Level", owned ? $"Lv.{currentLevel}/{maxLevel}" : "--",
            14, owned ? Color.cyan : Color.gray,
            new Vector2(0.48f, 0f), new Vector2(0.56f, 1f), Vector2.zero, TextAnchor.MiddleCenter);

        // - Level button
        var minusBtn = CreateSmallButton(row, "-", new Vector2(0.58f, 0.15f), new Vector2(0.64f, 0.85f));
        minusBtn.interactable = owned && currentLevel > 1;
        minusBtn.onClick.AddListener(() =>
        {
            _playerStats.SetPassiveLevel(passive, currentLevel - 1);
            RefreshPassiveList();
        });

        // + Level button
        var plusBtn = CreateSmallButton(row, "+", new Vector2(0.66f, 0.15f), new Vector2(0.72f, 0.85f));
        plusBtn.interactable = owned && currentLevel < maxLevel;
        plusBtn.onClick.AddListener(() =>
        {
            _playerStats.SetPassiveLevel(passive, currentLevel + 1);
            RefreshPassiveList();
        });

        // Max button
        var maxBtn = CreateSmallButton(row, "MAX", new Vector2(0.74f, 0.15f), new Vector2(0.80f, 0.85f));
        maxBtn.interactable = owned && currentLevel < maxLevel;
        maxBtn.onClick.AddListener(() =>
        {
            _playerStats.SetPassiveLevel(passive, maxLevel);
            RefreshPassiveList();
        });

        // Add / Remove button
        var toggleBtn = CreateSmallButton(row,
            owned ? "移除" : "添加",
            new Vector2(0.84f, 0.1f), new Vector2(0.97f, 0.9f));
        var toggleImg = toggleBtn.GetComponent<Image>();
        if (toggleImg != null)
            toggleImg.color = owned ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.2f);

        toggleBtn.onClick.AddListener(() =>
        {
            if (owned)
            {
                _playerStats.RemovePassive(passive);
            }
            else
            {
                _playerStats.ApplyPassive(passive);
            }
            RefreshPassiveList();
        });
    }

    // ── UI Helper Utilities ─────────────────────────────────────

    private static Transform CreateRow(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(0f, 36f);
        var img = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.05f);

        var layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        layout.minHeight = 36f;
        return obj.transform;
    }

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

    private static Text CreateLabel(Transform parent, string name, string text, int fontSize,
        Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, TextAnchor alignment,
        out GameObject outObj)
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
        outObj = obj;
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
        img.color = new Color(0.25f, 0.55f, 0.25f, 0.9f);

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
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Color transition
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.75f, 0.35f);
        colors.pressedColor = new Color(0.2f, 0.5f, 0.2f);
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
        img.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

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
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.55f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
        btn.colors = colors;

        return btn;
    }

    private static Transform CreateScrollView(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = anchorMin;
        scrollRect.anchorMax = anchorMax;
        scrollRect.sizeDelta = Vector2.zero;

        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.08f, 0.08f, 0.1f, 0.6f);

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        var vpMask = viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = vpRect;
        scroll.horizontal = false;
        scroll.vertical = true;

        return content.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
