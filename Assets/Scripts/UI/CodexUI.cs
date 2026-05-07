using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Weapon and Character codex/browser UI.
/// Shows all weapons with evolution routes and all characters with unlock conditions.
/// Opened from the pause menu or main menu. All UI built programmatically.
/// </summary>
public class CodexUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Transform _contentArea;
    private int _currentTab; // 0 = Weapons, 1 = Characters

    private List<WeaponData> _allWeapons = new List<WeaponData>();
    private List<CharacterData> _allCharacters = new List<CharacterData>();

    private void Awake()
    {
        BuildUI();
        Hide();
    }

    private void Start()
    {
        LoadData();
        ShowTab(0);
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
        LoadData();
        ShowTab(_currentTab);
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

    // ── Data Loading ────────────────────────────────────────────

    private void LoadData()
    {
        _allWeapons.Clear();
        _allCharacters.Clear();
        _allWeapons.AddRange(Resources.FindObjectsOfTypeAll<WeaponData>());
        _allCharacters.AddRange(Resources.FindObjectsOfTypeAll<CharacterData>());
    }

    // ── Tab Switching ───────────────────────────────────────────

    private void ShowTab(int tab)
    {
        _currentTab = tab;
        ClearContent();

        if (tab == 0)
            ShowWeaponsTab();
        else
            ShowCharactersTab();
    }

    // ── Weapons Tab ─────────────────────────────────────────────

    private void ShowWeaponsTab()
    {
        foreach (var wd in _allWeapons)
        {
            if (wd == null) continue;
            CreateWeaponEntry(wd);
        }
    }

    private void CreateWeaponEntry(WeaponData wd)
    {
        var row = CreateRow(_contentArea, $"Weapon_{wd.weaponName}");

        // Icon
        if (wd.icon != null)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.1f);
            iconRect.anchorMax = new Vector2(0.08f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = wd.icon;
            iconImg.preserveAspect = true;
        }

        // Name + type
        string typeTag = GetTypeTag(wd);
        CreateLabel(row, "Name", $"{wd.weaponName} {typeTag}", 16, Color.white,
            new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.9f), new Vector2(4, 0), TextAnchor.MiddleLeft);

        // Evolution info
        string evoInfo = "";
        if (wd.canEvolve && !string.IsNullOrEmpty(wd.requiredPassiveId) && wd.evolvedWeapon != null)
            evoInfo = $"→ {wd.evolvedWeapon.weaponName} (需要 {wd.requiredPassiveId})";
        else if (wd.isEvolutionOnly)
            evoInfo = "⚡ 进化武器";

        if (!string.IsNullOrEmpty(evoInfo))
        {
            CreateLabel(row, "Evo", evoInfo, 12, new Color(0.8f, 0.7f, 0.3f),
                new Vector2(0.52f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(4, 0), TextAnchor.MiddleLeft);
        }
    }

    private static string GetTypeTag(WeaponData wd)
    {
        if (wd == null) return "";
        switch (wd.weaponType)
        {
            case WeaponData.WeaponType.Projectile: return "[投射]";
            case WeaponData.WeaponType.Orbital: return "[轨道]";
            case WeaponData.WeaponType.Area: return "[范围]";
            case WeaponData.WeaponType.Auxiliary: return "[辅助]";
            default: return "";
        }
    }

    // ── Characters Tab ──────────────────────────────────────────

    private void ShowCharactersTab()
    {
        foreach (var cd in _allCharacters)
        {
            if (cd == null) continue;
            CreateCharacterEntry(cd);
        }
    }

    private void CreateCharacterEntry(CharacterData cd)
    {
        var row = CreateRow(_contentArea, $"Char_{cd.characterName}");

        bool unlocked = cd.isDefaultUnlocked ||
            (UnlockManager.HasInstance && UnlockManager.Instance.IsUnlocked(cd.id));

        // Portrait
        if (cd.portrait != null)
        {
            var iconObj = new GameObject("Portrait");
            iconObj.transform.SetParent(row, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.05f);
            iconRect.anchorMax = new Vector2(0.08f, 0.95f);
            iconRect.sizeDelta = Vector2.zero;
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = cd.portrait;
            iconImg.preserveAspect = true;
            if (!unlocked) iconImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        // Name
        var nameColor = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        CreateLabel(row, "Name", unlocked ? cd.characterName : "???", 16, nameColor,
            new Vector2(0.1f, 0.5f), new Vector2(0.45f, 0.9f), new Vector2(4, 0), TextAnchor.MiddleLeft);

        // Stats
        if (unlocked)
        {
            string stats = $"HP:{cd.baseHP} 速度:{cd.moveSpeed:F1} 护甲:{cd.armor}";
            CreateLabel(row, "Stats", stats, 12, new Color(0.7f, 0.7f, 0.75f),
                new Vector2(0.1f, 0.05f), new Vector2(0.6f, 0.5f), new Vector2(4, 0), TextAnchor.MiddleLeft);
        }
        else if (cd.unlockCondition != null)
        {
            string cond = cd.unlockCondition.Description;
            CreateLabel(row, "Cond", $"🔒 {cond}", 12, new Color(0.7f, 0.5f, 0.3f),
                new Vector2(0.1f, 0.05f), new Vector2(0.6f, 0.5f), new Vector2(4, 0), TextAnchor.MiddleLeft);
        }

        // Starting weapon
        if (unlocked && cd.startingWeapon != null)
        {
            CreateLabel(row, "Weapon", $"起始: {cd.startingWeapon.weaponName}", 12, new Color(0.5f, 0.8f, 1f),
                new Vector2(0.62f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(4, 0), TextAnchor.MiddleLeft);
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        var rootObj = new GameObject("CodexUI");
        rootObj.transform.SetParent(transform, false);
        rootObj.transform.SetAsLastSibling();
        var rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        _canvasGroup = rootObj.AddComponent<CanvasGroup>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(rootObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.97f);

        // Title
        CreateLabel(rootObj.transform, "Title", "📖 图鉴", 32, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.92f), new Vector2(1f, 0.98f), Vector2.zero, TextAnchor.MiddleCenter);

        // Tab buttons
        var weaponTabBtn = CreateTabButton(rootObj.transform, "WeaponTab", "⚔ 武器",
            new Vector2(0.1f, 0.85f), new Vector2(0.45f, 0.91f));
        weaponTabBtn.onClick.AddListener(() => ShowTab(0));

        var charTabBtn = CreateTabButton(rootObj.transform, "CharTab", "🧙 角色",
            new Vector2(0.55f, 0.85f), new Vector2(0.9f, 0.91f));
        charTabBtn.onClick.AddListener(() => ShowTab(1));

        // Content area (scrollable)
        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(rootObj.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.08f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.83f);
        scrollRect.sizeDelta = Vector2.zero;
        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.08f, 0.08f, 0.1f, 0.6f);

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;

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

        _contentArea = content.transform;

        // Close button
        var closeBtn = CreateTabButton(rootObj.transform, "CloseBtn", "✕ 关闭",
            new Vector2(0.35f, 0.01f), new Vector2(0.65f, 0.07f));
        var closeImg = closeBtn.GetComponent<Image>();
        if (closeImg != null) closeImg.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);
        closeBtn.onClick.AddListener(Hide);
    }

    // ── UI Helpers ──────────────────────────────────────────────

    private void ClearContent()
    {
        for (int i = _contentArea.childCount - 1; i >= 0; i--)
            Destroy(_contentArea.GetChild(i).gameObject);
    }

    private static Transform CreateRow(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(0f, 48f);
        var img = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.05f);
        var layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = 48f;
        layout.minHeight = 48f;
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

    private static Button CreateTabButton(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.35f, 0.9f);
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
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.55f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.3f);
        btn.colors = colors;
        return btn;
    }
}
