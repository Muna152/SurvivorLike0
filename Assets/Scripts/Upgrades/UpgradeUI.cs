using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI upgrade selection UI: shows 3 cards on level-up, pauses game until selection.
/// Uses CanvasGroup so the panel stays active (Start() always runs) while being invisible.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private Transform _cardContainer;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _skipButton;

    private CanvasGroup _canvasGroup;
    private UpgradeManager _manager;
    private List<UpgradeOption> _options;
    private readonly List<GameObject> _cardInstances = new List<GameObject>();

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        _manager = FindObjectOfType<UpgradeManager>();
        if (_manager != null)
        {
            _manager.OnOptionsGenerated += Show;
            _manager.OnUpgradeComplete += Hide;
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(OnSkipClicked);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.OnOptionsGenerated -= Show;
            _manager.OnUpgradeComplete -= Hide;
        }
    }

    private void Show(List<UpgradeOption> options)
    {
        _options = options;
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        if (_titleText != null) _titleText.text = "LEVEL UP!";

        ClearCards();

        for (int i = 0; i < options.Count; i++)
        {
            var cardObj = Instantiate(_cardPrefab, _cardContainer);
            _cardInstances.Add(cardObj);

            var card = cardObj.GetComponent<UpgradeCard>();
            if (card != null)
            {
                int idx = i;
                card.Setup(options[i], () => OnCardSelected(idx));
            }
        }
    }

    private void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        ClearCards();
    }

    private void ClearCards()
    {
        foreach (var c in _cardInstances)
        {
            if (c != null) Destroy(c);
        }
        _cardInstances.Clear();
    }

    private void OnCardSelected(int index)
    {
        if (_manager != null && index >= 0 && index < _options.Count)
        {
            _manager.OnOptionSelected(_options[index]);
        }
    }

    private void OnSkipClicked()
    {
        if (_manager != null)
        {
            _manager.SkipUpgrade();
        }
    }
}