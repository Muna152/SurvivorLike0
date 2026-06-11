using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects enemy contact and applies damage with invincibility frames.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private float _invincibleDuration = 0.5f;
    [SerializeField] private float _damageInterval = 0.5f;

    private PlayerStats _stats;
    private SpriteRenderer _sr;
    private float _invincibleTimer;
    private readonly Dictionary<int, float> _enemyDamageTimers = new Dictionary<int, float>();
    private DifficultyManager _difficultyManager; // Cached to avoid repeated Singleton lock in hot path

    // Cached WaitForSeconds to avoid allocation in FlashCoroutine
    private WaitForSeconds _flashWait;

    // Stale-entry cleanup to prevent indefinite dictionary growth
    private float _cleanupTimer;
    private const float CleanupInterval = 5f;    // seconds between cleanups
    private const float EntryMaxAge = 10f;       // entries older than this are removed
    private readonly List<int> _staleKeyBuffer = new List<int>(16);

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _sr = GetComponent<SpriteRenderer>();
        _difficultyManager = DifficultyManager.HasInstance ? DifficultyManager.Instance : null;
        _flashWait = new WaitForSeconds(0.05f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_invincibleTimer > 0f) return;

        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;

        int id = enemy.GetInstanceID();
        float now = Time.time;
        if (_enemyDamageTimers.TryGetValue(id, out float last) && now - last < _damageInterval)
            return;

        _enemyDamageTimers[id] = now;
        float dmgScale = _difficultyManager != null ? _difficultyManager.DamageMultiplier : 1f;
        _stats.TakeDamage(Mathf.CeilToInt(enemy.Data.damage * dmgScale));
        _invincibleTimer = _invincibleDuration;

        if (_sr != null) StartCoroutine(FlashCoroutine());
    }

    private void Update()
    {
        if (_invincibleTimer > 0f)
        {
            _invincibleTimer -= Time.deltaTime;
        }

        // Periodic cleanup of stale enemy damage timers
        _cleanupTimer += Time.deltaTime;
        if (_cleanupTimer >= CleanupInterval)
        {
            _cleanupTimer = 0f;
            CleanupStaleEntries();
        }
    }

    private void CleanupStaleEntries()
    {
        if (_enemyDamageTimers.Count == 0) return;

        float threshold = Time.time - EntryMaxAge;
        _staleKeyBuffer.Clear();

        foreach (var kvp in _enemyDamageTimers)
        {
            if (kvp.Value < threshold)
                _staleKeyBuffer.Add(kvp.Key);
        }

        for (int i = 0; i < _staleKeyBuffer.Count; i++)
            _enemyDamageTimers.Remove(_staleKeyBuffer[i]);
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _invincibleDuration)
        {
            _sr.enabled = false;
            yield return _flashWait;
            _sr.enabled = true;
            yield return _flashWait;
            elapsed += 0.1f;
        }
    }
}