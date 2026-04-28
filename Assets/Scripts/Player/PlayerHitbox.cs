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

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _sr = GetComponent<SpriteRenderer>();
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
        _stats.TakeDamage((int)enemy.Data.damage);
        _invincibleTimer = _invincibleDuration;

        if (_sr != null) StartCoroutine(FlashCoroutine());
    }

    private void Update()
    {
        if (_invincibleTimer > 0f)
        {
            _invincibleTimer -= Time.deltaTime;
        }
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _invincibleDuration)
        {
            _sr.enabled = false;
            yield return new WaitForSeconds(0.05f);
            _sr.enabled = true;
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.1f;
        }
    }
}