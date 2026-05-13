using UnityEngine;

/// <summary>
/// Utility component that adds fade-in/fade-out animation to a CanvasGroup.
/// Attach to the same GameObject as a CanvasGroup, then call FadeIn/FadeOut
/// instead of directly setting alpha.
/// Uses unscaledDeltaTime so it works when timeScale=0 (pause, upgrade, etc.).
/// </summary>
public class UIFadeAnimator : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private float _fadeAlpha;
    private float _targetAlpha;
    private float _fadeSpeed;
    private bool _isFading;

    [SerializeField] private float _defaultFadeDuration = 0.25f;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _fadeAlpha = _canvasGroup.alpha;
        _targetAlpha = _fadeAlpha;
    }

    private void Update()
    {
        if (!_isFading) return;

        float speed = _fadeSpeed > 0f ? _fadeSpeed : (_defaultFadeDuration > 0f ? 1f / _defaultFadeDuration : 4f);
        _fadeAlpha = Mathf.MoveTowards(_fadeAlpha, _targetAlpha, speed * Time.unscaledDeltaTime);

        _canvasGroup.alpha = _fadeAlpha;

        if (Mathf.Approximately(_fadeAlpha, _targetAlpha))
        {
            _fadeAlpha = _targetAlpha;
            _canvasGroup.alpha = _targetAlpha;
            _isFading = false;
        }
    }

    /// <summary>Fade the CanvasGroup to visible (alpha=1) with the default duration.</summary>
    public void FadeIn()
    {
        FadeTo(1f, _defaultFadeDuration);
    }

    /// <summary>Fade the CanvasGroup to visible with a custom duration.</summary>
    public void FadeIn(float duration)
    {
        FadeTo(1f, duration);
    }

    /// <summary>Fade the CanvasGroup to invisible (alpha=0) with the default duration.</summary>
    public void FadeOut()
    {
        FadeTo(0f, _defaultFadeDuration);
    }

    /// <summary>Fade the CanvasGroup to invisible with a custom duration.</summary>
    public void FadeOut(float duration)
    {
        FadeTo(0f, duration);
    }

    /// <summary>Fade to a specific alpha value.</summary>
    public void FadeTo(float targetAlpha, float duration)
    {
        _targetAlpha = targetAlpha;
        _fadeSpeed = duration > 0f ? 1f / duration : 1000f;
        _isFading = true;

        // If we're fading to visible, enable interaction immediately
        if (targetAlpha >= 1f)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
        // If we're fading to invisible, disable interaction immediately
        else if (targetAlpha <= 0f)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    /// <summary>Skip the fade animation and set alpha immediately.</summary>
    public void SkipFade(float alpha)
    {
        _fadeAlpha = alpha;
        _targetAlpha = alpha;
        _canvasGroup.alpha = alpha;
        _canvasGroup.blocksRaycasts = alpha >= 1f;
        _canvasGroup.interactable = alpha >= 1f;
        _isFading = false;
    }

    /// <summary>Whether currently animating a fade.</summary>
    public bool IsFading => _isFading;
}
