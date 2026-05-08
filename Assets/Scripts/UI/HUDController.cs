using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD controller: HP bar, EXP bar, timer, and weapon icon bar.
/// Uses event-driven refresh instead of polling every frame to reduce GC.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("HP Bar")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Text _hpText;

    [Header("EXP Bar")]
    [SerializeField] private Slider _expSlider;
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _expText;

    [Header("Timer")]
    [SerializeField] private Text _timerText;

    [Header("Weapon Bar")]
    [SerializeField] private Transform _weaponBarContainer;
    [SerializeField] private GameObject _weaponSlotPrefab;

    private PlayerStats _stats;
    private PlayerWeaponManager _weaponManager;

    // Cached values to detect changes and avoid redundant UI updates
    private float _lastHPDisplay = -1f;
    private float _lastMaxHPDisplay = -1f;
    private int _lastLevel = -1;
    private int _lastTimerSec = -1;

    // Cached delegates to allow proper unsubscribe
    private System.Action<int> _onDamagedHandler;
    private System.Action<float> _onHealedHandler;
    private System.Action<int> _onLevelUpHandler;

    private void Start()
    {
        // Ensure MainMenuUI exists on the Canvas (it may be missing after scene reload)
        if (GetComponent<MainMenuUI>() == null)
            gameObject.AddComponent<MainMenuUI>();

        _stats = FindObjectOfType<PlayerStats>();
        _weaponManager = FindObjectOfType<PlayerWeaponManager>();

        // Create cached delegate instances so unsubscribe works correctly
        _onDamagedHandler = _ => RefreshHP();
        _onHealedHandler = _ => RefreshHP();
        _onLevelUpHandler = _ => OnLevelUp();

        GameEvents.OnPlayerDamaged += _onDamagedHandler;
        GameEvents.OnPlayerHealed += _onHealedHandler;
        GameEvents.OnPlayerLevelUp += _onLevelUpHandler;

        // Initial refresh
        RefreshHP();
        RefreshEXP();
        RefreshTimer();
    }

    private void OnDestroy()
    {
        GameEvents.OnPlayerDamaged -= _onDamagedHandler;
        GameEvents.OnPlayerHealed -= _onHealedHandler;
        GameEvents.OnPlayerLevelUp -= _onLevelUpHandler;
    }

    private void Update()
    {
        // Only refresh timer every frame (cheap int compare)
        RefreshTimer();

        // Refresh HP/EXP only if values changed (avoids string allocation every frame)
        RefreshHPIfNeeded();
        RefreshEXPIfNeeded();
    }

    private void RefreshHP()
    {
        if (_stats == null) return;

        if (_hpSlider != null)
        {
            _hpSlider.maxValue = _stats.MaxHP;
            _hpSlider.value = Mathf.Min(_stats.CurrentHP, _stats.MaxHP);
        }

        if (_hpText != null)
        {
            int hp = (int)_stats.CurrentHP;
            int maxHp = (int)_stats.MaxHP;
            _hpText.text = $"HP: {hp}/{maxHp}";
        }

        _lastHPDisplay = _stats.CurrentHP;
        _lastMaxHPDisplay = _stats.MaxHP;
    }

    private void RefreshHPIfNeeded()
    {
        if (_stats == null) return;

        float hp = _stats.CurrentHP;
        float maxHp = _stats.MaxHP;
        if (Mathf.Approximately(hp, _lastHPDisplay) && Mathf.Approximately(maxHp, _lastMaxHPDisplay))
            return;

        if (_hpSlider != null)
        {
            _hpSlider.maxValue = maxHp;
            _hpSlider.value = Mathf.Min(hp, maxHp);
        }

        if (_hpText != null)
            _hpText.text = $"HP: {(int)hp}/{(int)maxHp}";

        _lastHPDisplay = hp;
        _lastMaxHPDisplay = maxHp;
    }

    private void RefreshEXP()
    {
        if (_stats == null) return;

        if (_expSlider != null)
            _expSlider.value = _stats.EXPProgress;

        if (_levelText != null)
        {
            _levelText.text = $"Lv.{_stats.Level}";
            _lastLevel = _stats.Level;
        }

        UpdateExpText();
    }

    private void RefreshEXPIfNeeded()
    {
        if (_stats == null) return;

        if (_levelText != null && _stats.Level != _lastLevel)
        {
            _levelText.text = $"Lv.{_stats.Level}";
            _lastLevel = _stats.Level;
        }

        if (_expSlider != null)
            _expSlider.value = _stats.EXPProgress;

        UpdateExpText();
    }

    private void UpdateExpText()
    {
        if (_expText == null) return;
        _expText.text = $"{(int)_stats.CurrentEXP}/{(int)_stats.EXPToNextLevel}";
    }

    private void OnLevelUp()
    {
        RefreshEXP();
        RefreshWeaponBar();
    }

    private void RefreshTimer()
    {
        if (_timerText == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        float t = gm.ElapsedTime;
        int totalSec = (int)t;
        if (totalSec == _lastTimerSec) return; // no change, skip string alloc

        _lastTimerSec = totalSec;
        int min = totalSec / 60;
        int sec = totalSec % 60;
        _timerText.text = $"{min:D2}:{sec:D2}";

        if (min < 10) _timerText.color = Color.white;
        else if (min < 20) _timerText.color = Color.yellow;
        else _timerText.color = Color.red;
    }

    private void RefreshWeaponBar()
    {
        if (_weaponBarContainer == null || _weaponManager == null) return;

        for (int i = _weaponBarContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_weaponBarContainer.GetChild(i).gameObject);
        }

        foreach (var weapon in _weaponManager.EquippedWeapons)
        {
            var slotObj = Instantiate(_weaponSlotPrefab, _weaponBarContainer);
            var icon = slotObj.GetComponentInChildren<Image>();
            var text = slotObj.GetComponentInChildren<Text>();

            if (icon != null && weapon.Data.icon != null)
                icon.sprite = weapon.Data.icon;

            if (text != null)
                text.text = $"Lv.{weapon.CurrentLevel}";
        }
    }
}
