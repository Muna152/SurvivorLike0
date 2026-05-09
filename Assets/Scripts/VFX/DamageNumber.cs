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
    private string _poolKey;

    private void Awake()
    {
        _tm = GetComponent<TextMesh>();
        _poolKey = "DamageNumber";
    }

    /// <summary>
    /// Show a damage number at the given position.
    /// </summary>
    /// <param name="position">World position to spawn at</param>
    /// <param name="amount">Damage amount to display</param>
    /// <param name="color">Text color (white=normal, yellow=critical, red=player)</param>
    public void Show(Vector3 position, int amount, Color color)
    {
        transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0f);
        _tm.text = amount.ToString();
        _tm.color = color;
        _timer = 0f;
        _playing = true;
        transform.localScale = Vector3.one * _startScale;
        gameObject.SetActive(true);
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
