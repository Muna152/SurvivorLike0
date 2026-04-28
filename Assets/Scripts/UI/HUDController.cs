using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD controller: HP bar, EXP bar, timer, and weapon icon bar.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("HP Bar")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Text _hpText;

    [Header("EXP Bar")]
    [SerializeField] private Slider _expSlider;
    [SerializeField] private Text _levelText;

    [Header("Timer")]
    [SerializeField] private Text _timerText;

    [Header("Weapon Bar")]
    [SerializeField] private Transform _weaponBarContainer;
    [SerializeField] private GameObject _weaponSlotPrefab;

    private PlayerStats _stats;
    private PlayerWeaponManager _weaponManager;

    private void Start()
    {
        _stats = FindObjectOfType<PlayerStats>();
        _weaponManager = FindObjectOfType<PlayerWeaponManager>();

        GameEvents.OnPlayerDamaged += _ => RefreshHP();
        GameEvents.OnPlayerLevelUp += _ => RefreshLevel();
    }

    private void OnDestroy()
    {
        GameEvents.OnPlayerDamaged -= _ => RefreshHP();
        GameEvents.OnPlayerLevelUp -= _ => RefreshLevel();
    }

    private void Update()
    {
        RefreshHP();
        RefreshEXP();
        RefreshTimer();
    }

    private void RefreshHP()
    {
        if (_stats == null) return;
        if (_hpSlider != null)
        {
            _hpSlider.maxValue = _stats.MaxHP;
            _hpSlider.value = _stats.CurrentHP;
        }
        if (_hpText != null)
        {
            _hpText.text = $"HP: {(int)_stats.CurrentHP}/{(int)_stats.MaxHP}";
        }
    }

    private void RefreshEXP()
    {
        if (_stats == null) return;
        if (_expSlider != null)
        {
            _expSlider.maxValue = 1f;
            _expSlider.value = _stats.EXPProgress;
        }
        if (_levelText != null)
        {
            _levelText.text = $"Lv.{_stats.Level}";
        }
    }

    private void RefreshLevel()
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
        int min = (int)(t / 60f);
        int sec = (int)(t % 60f);
        _timerText.text = $"{min:D2}:{sec:D2}";

        // Color shift
        if (min < 10) _timerText.color = Color.white;
        else if (min < 20) _timerText.color = Color.yellow;
        else _timerText.color = Color.red;
    }

    private void RefreshWeaponBar()
    {
        if (_weaponBarContainer == null || _weaponManager == null) return;

        // Clear existing
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