using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene fade transition system. Creates a persistent black overlay that
/// fades in/out when transitioning between scenes.
/// Uses DontDestroyOnLoad so it survives scene loads.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition _instance;
    public static SceneTransition Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SceneTransition>();
                if (_instance == null)
                    CreateInstance();
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    private CanvasGroup _canvasGroup;
    private Image _overlayImage;
    private bool _isTransitioning;

    /// <summary>Whether a transition is currently in progress.</summary>
    public bool IsTransitioning => _isTransitioning;

    private const float DEFAULT_DURATION = 0.4f;

    private static void CreateInstance()
    {
        var obj = new GameObject("[SceneTransition]");
        DontDestroyOnLoad(obj);
        _instance = obj.AddComponent<SceneTransition>();
        _instance.Initialize();
    }

    private void Initialize()
    {
        // Create Canvas
        var canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Above everything
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // CanvasGroup on the canvas
        _canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        // Black overlay Image
        var imgObj = new GameObject("FadeOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        var imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.sizeDelta = Vector2.zero;
        _overlayImage = imgObj.AddComponent<Image>();
        _overlayImage.color = Color.black;
    }

    // ── Public API ─────────────────────────────────────────────

    /// <summary>Transition to a scene with fade-out then fade-in.</summary>
    /// <param name="sceneName">Scene name to load.</param>
    /// <param name="onMidpoint">Optional callback at the midpoint (scene load).</param>
    /// <param name="duration">Duration for each half of the transition (fade-out + fade-in).</param>
    public void TransitionToScene(string sceneName, Action onMidpoint = null, float duration = DEFAULT_DURATION)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionSequence(sceneName, onMidpoint, duration));
    }

    /// <summary>Transition to a scene by build index.</summary>
    public void TransitionToScene(int buildIndex, Action onMidpoint = null, float duration = DEFAULT_DURATION)
    {
        if (_isTransitioning) return;
        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        TransitionToScene(sceneName, onMidpoint, duration);
    }

    /// <summary>Fade in from black (scene appearing). Called manually if needed.</summary>
    public void FadeIn(Action onComplete = null, float duration = DEFAULT_DURATION)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 1f;
        StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
    }

    /// <summary>Fade out to black (scene disappearing). Called manually if needed.</summary>
    public void FadeOut(Action onComplete = null, float duration = DEFAULT_DURATION)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 0f;
        StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
    }

    // ── Transition Sequence ─────────────────────────────────────

    private System.Collections.IEnumerator TransitionSequence(string sceneName, Action onMidpoint, float duration)
    {
        _isTransitioning = true;

        // Block input during transition
        _canvasGroup.blocksRaycasts = true;

        // Phase 1: Fade out to black
        yield return FadeRoutine(0f, 1f, duration, null);

        // Phase 2: Load scene
        onMidpoint?.Invoke();
        SceneManager.LoadScene(sceneName);

        // Wait one frame for the new scene to initialize
        yield return null;

        // Phase 3: Fade in from black
        yield return FadeRoutine(1f, 0f, duration, null);

        // Done
        _canvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    private System.Collections.IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            // Smooth step for nicer easing
            t = t * t * (3f - 2f * t);
            _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = toAlpha;
        onComplete?.Invoke();
    }
}
