using UnityEngine;

/// <summary>
/// Floating damage number that rises and fades out.
/// Uses TextMesh for world-space rendering. No coroutines.
/// </summary>
[RequireComponent(typeof(TextMesh))]
public class DamageNumber : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private float _riseSpeed = 2f;
    [SerializeField] private float _startScale = 1.2f;
    [SerializeField] private float _endScale = 0.6f;

    private TextMesh _tm;
    private float _timer;
    private bool _playing;
    private int _currentAmount;
    private string _poolKey;

    private void Awake()
    {
        _tm = GetComponent<TextMesh>();
        _poolKey = "DamageNumber";
    }

    public bool IsPlaying => _playing;
    public int CurrentAmount => _currentAmount;
    public float ElapsedTime => _timer;

    /// <summary>
    /// Show a damage number at the given position.
    /// </summary>
    public void Show(Vector3 position, int amount, Color color)
    {
        transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0f);
        _currentAmount = amount;
        _tm.text = amount.ToString();
        _tm.color = color;
        _timer = 0f;
        _playing = true;
        transform.localScale = Vector3.one * _startScale;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Add damage to an already-playing number, updating text in place.
    /// </summary>
    public void Accumulate(int additionalDamage, Vector3 newPosition)
    {
        _currentAmount += additionalDamage;
        _tm.text = _currentAmount.ToString();
        // Pop scale back up and extend life slightly
        float t = Mathf.Clamp01(_timer / _duration);
        transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _startScale * 0.9f, t);
        _timer = Mathf.Max(0f, _timer - 0.15f);
        // Keep alpha full (undo any fade)
        Color c = _tm.color;
        c.a = 1f;
        _tm.color = c;
        // Follow enemy position horizontally
        Vector3 pos = transform.position;
        pos.x = newPosition.x + Random.Range(-0.3f, 0.3f);
        transform.position = pos;
    }

    private void Update()
    {
        if (!_playing) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / _duration);

        // Rise upward
        transform.position += Vector3.up * _riseSpeed * Time.deltaTime;

        // Scale down
        float scale = Mathf.Lerp(_startScale, _endScale, t);
        transform.localScale = Vector3.one * scale;

        // Fade out in last 40%
        if (t > 0.6f)
        {
            float fadeT = (t - 0.6f) / 0.4f;
            Color c = _tm.color;
            c.a = 1f - fadeT;
            _tm.color = c;
        }

        if (t >= 1f)
        {
            _playing = false;
            ReturnToPool();
        }
    }

    public void ResetForReuse()
    {
        _playing = false;
        _timer = 0f;
        if (_tm != null)
        {
            _tm.text = "";
            _tm.color = Color.white;
        }
        transform.localScale = Vector3.one;
    }

    private void ReturnToPool()
    {
        if (PoolManager.HasInstance)
            PoolManager.Instance.Return<DamageNumber>(_poolKey, this);
        else
            gameObject.SetActive(false);
    }
}
