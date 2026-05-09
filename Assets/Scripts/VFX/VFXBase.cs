using UnityEngine;

/// <summary>
/// Base component for pooled sprite-based VFX.
/// Animates scale and color (alpha) over a configurable duration, then returns to pool.
/// No coroutines — uses Update timer per project conventions.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class VFXBase : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _startScale = 0.5f;
    [SerializeField] private float _endScale = 2f;
    [SerializeField] private Color _startColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color _endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _alphaCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.7f, 0.8f),
        new Keyframe(1f, 0f));

    private SpriteRenderer _sr;
    private float _timer;
    private bool _playing;
    private string _poolKey;

    public bool IsPlaying => _playing;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _poolKey = gameObject.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// Play this VFX at the given world position.
    /// </summary>
    public void Play(Vector3 position)
    {
        transform.position = position;
        _timer = 0f;
        _playing = true;
        gameObject.SetActive(true);

        // Apply initial frame
        ApplyFrame(0f);
    }

    /// <summary>
    /// Play with a custom start color (preserves RGB, resets alpha to 1).
    /// </summary>
    public void Play(Vector3 position, Color tint)
    {
        _startColor = new Color(tint.r, tint.g, tint.b, 1f);
        _endColor = new Color(tint.r, tint.g, tint.b, 0f);
        Play(position);
    }

    private void Update()
    {
        if (!_playing) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / _duration);

        ApplyFrame(t);

        if (t >= 1f)
        {
            _playing = false;
            ReturnToPool();
        }
    }

    private void ApplyFrame(float t)
    {
        if (_sr == null) return;

        // Scale
        float scaleT = _scaleCurve.Evaluate(t);
        float scale = Mathf.LerpUnclamped(_startScale, _endScale, scaleT);
        transform.localScale = new Vector3(scale, scale, 1f);

        // Color (interpolate RGB and alpha separately for tint preservation)
        float alphaT = _alphaCurve.Evaluate(t);
        Color c = Color.LerpUnclamped(_startColor, _endColor, alphaT);
        _sr.color = c;
    }

    /// <summary>Stop immediately and return to pool.</summary>
    public void Stop()
    {
        _playing = false;
        ReturnToPool();
    }

    public void ResetForReuse()
    {
        _playing = false;
        _timer = 0f;
        transform.localScale = Vector3.one;
        if (_sr != null) _sr.color = Color.white;
    }

    private void ReturnToPool()
    {
        if (PoolManager.HasInstance)
            PoolManager.Instance.Return<VFXBase>(_poolKey, this);
        else
            gameObject.SetActive(false);
    }
}
