using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VS-like chest opening UI: pauses the game, plays chest-open animation,
/// reveals evolution/upgrade result, then resumes.
/// Uses GameManager.PushPause/PopPause for safe timeScale management.
/// All animation uses unscaledDeltaTime so it works when timeScale=0.
/// </summary>
public class ChestOpenUI : MonoBehaviour
{
    private enum Phase { Idle, ChestAppear, ChestBurst, ResultShow, FadeOut }

    [Header("Timing (unscaled seconds)")]
    [SerializeField] private float _chestAppearDuration = 0.3f;
    [SerializeField] private float _chestBurstDuration = 0.5f;
    [SerializeField] private float _resultShowDuration = 2.0f;
    [SerializeField] private float _fadeOutDuration = 0.3f;
    [SerializeField] private int _fallbackGold = 50;

    private Phase _phase;
    private float _timer;
    private CanvasGroup _canvasGroup;
    private GameObject _overlay;
    private GameObject _chestImage;
    private GameObject _burstFlash;
    private UnityEngine.UI.Image _resultIcon;
    private UnityEngine.UI.Text _resultText;
    private bool _isOpening;
    private int _pendingChests;

    private static ChestOpenUI _instance;
    public static ChestOpenUI Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<ChestOpenUI>();
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        BuildUI();
    }

    private void OnEnable()
    {
        GameEvents.OnChestCollected += OnChestCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnChestCollected -= OnChestCollected;
    }

    private void BuildUI()
    {
        // Create a dedicated child Canvas so we don't interfere with the HUD Canvas
        var canvasObj = new GameObject("ChestOpenCanvas");
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above everything

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // CanvasGroup for fade (on our own canvas, not the HUD's)
        _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        // Overlay (semi-transparent dark background)
        _overlay = new GameObject("ChestOverlay");
        _overlay.transform.SetParent(canvasObj.transform, false);
        var overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImg = _overlay.AddComponent<UnityEngine.UI.Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Chest image (centered)
        _chestImage = new GameObject("ChestIcon");
        _chestImage.transform.SetParent(_overlay.transform, false);
        var chestRect = _chestImage.AddComponent<RectTransform>();
        chestRect.sizeDelta = new Vector2(120, 120);
        chestRect.anchorMin = new Vector2(0.5f, 0.5f);
        chestRect.anchorMax = new Vector2(0.5f, 0.5f);
        chestRect.pivot = new Vector2(0.5f, 0.5f);
        chestRect.anchoredPosition = Vector2.zero;
        var chestSprite = DropManager.HasInstance ? DropManager.Instance.ChestSprite : null;
        var chestImg = _chestImage.AddComponent<UnityEngine.UI.Image>();
        if (chestSprite != null)
            chestImg.sprite = chestSprite;
        chestImg.color = Color.white;
        _chestImage.SetActive(false);

        // Burst flash (full-screen white flash for burst moment)
        _burstFlash = new GameObject("BurstFlash");
        _burstFlash.transform.SetParent(_overlay.transform, false);
        var flashRect = _burstFlash.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        var flashImg = _burstFlash.AddComponent<UnityEngine.UI.Image>();
        flashImg.color = new Color(1f, 1f, 0.8f, 0f);
        _burstFlash.SetActive(false);

        // Result icon (weapon/item sprite above the text)
        var iconObj = new GameObject("ResultIcon");
        iconObj.transform.SetParent(_overlay.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, -30f);
        _resultIcon = iconObj.AddComponent<UnityEngine.UI.Image>();
        _resultIcon.color = Color.white;
        iconObj.SetActive(false);

        // Result text (below the icon)
        var textObj = new GameObject("ResultText");
        textObj.transform.SetParent(_overlay.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 80);
        textRect.anchoredPosition = new Vector2(0f, -100f);
        _resultText = textObj.AddComponent<UnityEngine.UI.Text>();
        _resultText.alignment = TextAnchor.MiddleCenter;
        _resultText.fontSize = 36;
        _resultText.color = Color.white;
        _resultText.fontStyle = FontStyle.Bold;
        _resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _resultText.text = "";

        _overlay.SetActive(false);
    }

    private void OnChestCollected()
    {
        if (_isOpening)
        {
            // Already opening — queue this chest for later
            _pendingChests++;
            return;
        }
        StartCoroutine(ChestOpenSequence());
    }

    private IEnumerator ChestOpenSequence()
    {
        _isOpening = true;

        // Pause game via reference-counted system
        GameManager.Instance.PushPause();

        // Show overlay
        _overlay.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        // ── Phase 1: Chest Appear ──────────────────────────────
        _phase = Phase.ChestAppear;
        _chestImage.SetActive(true);
        SetChestScale(0f);
        _resultText.text = "";
        _resultIcon.gameObject.SetActive(false);
        _burstFlash.SetActive(false);

        _timer = 0f;
        while (_timer < _chestAppearDuration)
        {
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / _chestAppearDuration);
            float scale = EaseOutBack(t);
            SetChestScale(scale);
            yield return null;
        }
        SetChestScale(1f);

        // ── Phase 2: Chest Burst ──────────────────────────────
        _phase = Phase.ChestBurst;

        // Determine result BEFORE burst so we know what to reveal
        bool isEvolution = false;
        Sprite rewardSprite = null;
        string resultMessage = DetermineResult(ref isEvolution, out rewardSprite);

        // Play VFX and SFX at the burst moment
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            VFXManager.Instance.PlayChestOpenEffect(player.transform.position);
        if (AudioManager.HasInstance)
            AudioManager.Instance.PlayChestOpenSFX();

        _burstFlash.SetActive(true);

        _timer = 0f;
        while (_timer < _chestBurstDuration)
        {
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / _chestBurstDuration);
            SetChestScale(1f + 0.3f * t);
            SetFlashAlpha(Mathf.Lerp(0.8f, 0f, t));
            yield return null;
        }
        SetChestScale(1.3f);
        SetFlashAlpha(0f);
        _burstFlash.SetActive(false);
        _chestImage.SetActive(false);

        // ── Phase 3: Result Show ───────────────────────────────
        _phase = Phase.ResultShow;

        // Show reward icon
        if (rewardSprite != null)
        {
            _resultIcon.sprite = rewardSprite;
            _resultIcon.gameObject.SetActive(true);
        }
        else
        {
            _resultIcon.gameObject.SetActive(false);
        }

        _resultText.text = resultMessage;
        _resultText.color = isEvolution ? new Color(1f, 0.85f, 0f) : new Color(0.5f, 1f, 0.8f);

        _timer = 0f;
        while (_timer < _resultShowDuration)
        {
            _timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // ── Phase 4: Fade Out ──────────────────────────────────
        _phase = Phase.FadeOut;
        _timer = 0f;
        while (_timer < _fadeOutDuration)
        {
            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / _fadeOutDuration);
            _canvasGroup.alpha = 1f - t;
            yield return null;
        }

        // ── Cleanup ───────────────────────────────────────────
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _overlay.SetActive(false);
        _resultText.text = "";
        _resultIcon.gameObject.SetActive(false);
        _chestImage.SetActive(false);
        _burstFlash.SetActive(false);
        _phase = Phase.Idle;

        // Resume game via reference-counted system
        GameManager.Instance.PopPause();
        _isOpening = false;

        // Process queued chests
        if (_pendingChests > 0)
        {
            _pendingChests--;
            StartCoroutine(ChestOpenSequence());
        }
    }

    private string DetermineResult(ref bool isEvolution, out Sprite rewardSprite)
    {
        isEvolution = false;
        rewardSprite = null;
        var pwm = FindObjectOfType<PlayerWeaponManager>();
        if (pwm == null) return "";

        // Priority 1: Weapon evolution
        var evolved = pwm.CheckAndEvolveWeapons();
        if (evolved != null && evolved.Count > 0)
        {
            isEvolution = true;
            var names = new List<string>();
            foreach (var w in evolved)
            {
                names.Add(w.Data.weaponName);
                if (rewardSprite == null && w.Data.icon != null)
                    rewardSprite = w.Data.icon;
            }
            return $"✨ 进化: {string.Join(", ", names)} ✨";
        }

        // Priority 2: Random weapon upgrade
        var upgraded = pwm.UpgradeRandomWeapon();
        if (upgraded != null)
        {
            rewardSprite = upgraded.Data.icon;
            return $"⬆ {upgraded.Data.weaponName} Lv{upgraded.CurrentLevel}";
        }

        // Priority 3: Fallback gold
        var stats = FindObjectOfType<PlayerStats>();
        if (stats != null)
            stats.AddGold(_fallbackGold);
        return $"💰 +{_fallbackGold} 金币";
    }

    private void SetChestScale(float scale)
    {
        if (_chestImage != null)
            _chestImage.transform.localScale = Vector3.one * scale;
    }

    private void SetFlashAlpha(float alpha)
    {
        if (_burstFlash != null)
        {
            var img = _burstFlash.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
