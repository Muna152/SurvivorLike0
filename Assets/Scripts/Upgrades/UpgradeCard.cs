using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// A single upgrade card in the UpgradeUI.
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _descText;
    [SerializeField] private Button _button;

    private UnityAction _onSelected;

    public void Setup(UpgradeOption option, UnityAction onSelected)
    {
        _onSelected = onSelected;

        if (_nameText != null) _nameText.text = option.Name;
        if (_descText != null) _descText.text = option.Description;
        if (_iconImage != null && option.Icon != null) _iconImage.sprite = option.Icon;

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        _onSelected?.Invoke();
    }
}