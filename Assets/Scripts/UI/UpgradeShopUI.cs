using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Permanent upgrade shop UI. Built programmatically (no prefab required).
/// Shows gold balance and 5 upgrade rows with level, cost, and purchase buttons.
/// Uses CanvasGroup for show/hide.
/// </summary>
public class UpgradeShopUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Text _goldText;
    private Text[] _levelTexts;
    private Text[] _costTexts;
    private Button[] _buyButtons;
    private readonly int _upgradeCount = System.Enum.GetValues(typeof(GoldManager.PermanentUpgradeType)).Length;

    private void Awake()
    {
        BuildUI();
        UINavUtil.DisableAll(transform);
        Hide();
    }

    private void OnEnable()
    {
        GameEvents.OnPermanentUpgradePurchased += RefreshUI;
    }

    private void OnDisable()
    {
        GameEvents.OnPermanentUpgradePurchased -= RefreshUI;
    }

    // ── Public API ──────────────────────────────────────────────

    public void Show()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _canvasGroup.transform.SetAsLastSibling();
        }
        RefreshUI();
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

    // ── UI Refresh ──────────────────────────────────────────────

    private void RefreshUI()
    {
        // Update gold display
        if (_goldText != null)
            _goldText.text = $"💰 金币: {GoldManager.GetGold()}";

        // Update upgrade rows
        for (int i = 0; i < _upgradeCount; i++)
        {
            var type = (GoldManager.PermanentUpgradeType)i;
            int level = GoldManager.GetUpgradeLevel(type);
            int maxLevel = GoldManager.GetMaxLevel(type);
            int cost = GoldManager.GetUpgradeCost(type);
            bool maxed = level >= maxLevel;

            if (_levelTexts[i] != null)
            {
                string levelBar = "";
                for (int l = 0; l < maxLevel; l++)
                    levelBar += l < level ? "■" : "□";
                _levelTexts[i].text = levelBar;
            }

            if (_costTexts[i] != null)
            {
                _costTexts[i].text = maxed ? "✅ 已满级" : $"费用: {cost}";
            }

            if (_buyButtons[i] != null)
            {
                _buyButtons[i].interactable = !maxed && GoldManager.GetGold() >= cost;
                var btnText = _buyButtons[i].GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = maxed ? "已满" : "购买";
            }
        }
    }

    // ── UI Construction ─────────────────────────────────────────

    private void BuildUI()
    {
        // Root panel
        var rootObj = new GameObject("UpgradeShopUI");
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
        CreateLabel(rootObj.transform, "Title", "🛒 永久升级商店", 36, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.90f), new Vector2(1f, 0.96f), Vector2.zero, TextAnchor.MiddleCenter);

        // Gold display
        _goldText = CreateLabel(rootObj.transform, "GoldText", "", 22, new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0.84f), new Vector2(1f, 0.90f), Vector2.zero, TextAnchor.MiddleCenter);

        // Upgrade rows
        _levelTexts = new Text[_upgradeCount];
        _costTexts = new Text[_upgradeCount];
        _buyButtons = new Button[_upgradeCount];

        float rowTop = 0.78f;
        float rowHeight = 0.14f;
        float gap = 0.02f;

        for (int i = 0; i < _upgradeCount; i++)
        {
            var type = (GoldManager.PermanentUpgradeType)i;
            float yMax = rowTop - i * (rowHeight + gap);
            float yMin = yMax - rowHeight;
            BuildUpgradeRow(rootObj.transform, i, type, yMin, yMax);
        }

        // Close button
        var closeBtn = CreateSmallButton(rootObj.transform, "关闭",
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.08f));
        closeBtn.onClick.AddListener(Hide);
    }

    private void BuildUpgradeRow(Transform parent, int index, GoldManager.PermanentUpgradeType type,
        float yMin, float yMax)
    {
        var rowObj = new GameObject($"UpgradeRow_{type}");
        rowObj.transform.SetParent(parent, false);
        var rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.08f, yMin);
        rowRect.anchorMax = new Vector2(0.92f, yMax);
        rowRect.sizeDelta = Vector2.zero;

        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

        // Upgrade name + description
        string nameText = GoldManager.GetUpgradeName(type);
        string descText = GoldManager.GetUpgradeDescription(type);
        CreateLabel(rowObj.transform, "NameDesc", $"{nameText}  ({descText})", 16, Color.white,
            new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.95f), Vector2.zero, TextAnchor.MiddleLeft);

        // Level indicator (■□□□□)
        var levelObj = new GameObject("LevelText");
        levelObj.transform.SetParent(rowObj.transform, false);
        var lvlRect = levelObj.AddComponent<RectTransform>();
        lvlRect.anchorMin = new Vector2(0.02f, 0.05f);
        lvlRect.anchorMax = new Vector2(0.45f, 0.50f);
        lvlRect.sizeDelta = Vector2.zero;
        var lvlText = levelObj.AddComponent<Text>();
        lvlText.fontSize = 18;
        lvlText.color = new Color(1f, 0.85f, 0.3f);
        lvlText.alignment = TextAnchor.MiddleLeft;
        lvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _levelTexts[index] = lvlText;

        // Cost text
        var costObj = new GameObject("CostText");
        costObj.transform.SetParent(rowObj.transform, false);
        var costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.45f, 0.05f);
        costRect.anchorMax = new Vector2(0.72f, 0.50f);
        costRect.sizeDelta = Vector2.zero;
        var costText = costObj.AddComponent<Text>();
        costText.fontSize = 14;
        costText.color = new Color(0.8f, 0.8f, 0.85f);
        costText.alignment = TextAnchor.MiddleLeft;
        costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _costTexts[index] = costText;

        // Buy button
        var buyBtn = CreateSmallButton(rowObj.transform, "购买",
            new Vector2(0.76f, 0.1f), new Vector2(0.98f, 0.9f));
        buyBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 0.9f);
        var buyColors = buyBtn.colors;
        buyColors.highlightedColor = new Color(0.3f, 0.8f, 0.4f);
        buyColors.pressedColor = new Color(0.15f, 0.45f, 0.2f);
        buyBtn.colors = buyColors;

        int capturedIndex = index;
        buyBtn.onClick.AddListener(() => OnPurchase(capturedIndex));
        _buyButtons[index] = buyBtn;
    }

    private void OnPurchase(int index)
    {
        var type = (GoldManager.PermanentUpgradeType)index;
        if (GoldManager.PurchaseUpgrade(type))
        {
            RefreshUI();
        }
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
}
