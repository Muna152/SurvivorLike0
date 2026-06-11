using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down 2D movement via WASD / arrow keys.
/// Uses Rigidbody2D.velocity. Broadcasts position/direction to weapon manager.
/// Includes Auto-Pilot mode (toggle with P key) with AI-controlled movement.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PlayerStats _stats;
    private PlayerWeaponManager _weaponManager;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Vector2 _lastDirection = Vector2.down;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int FrontRunHash = Animator.StringToHash("frontRun");

    public Vector2 LastDirection => _lastDirection;

    /// <summary>Whether the current character's sprite faces right by default.</summary>
    private bool _faceRightByDefault = true;

    // ── Auto-Pilot ───────────────────────────────────────────────

    /// <summary>Global auto-pilot state. Toggle with P key.</summary>
    public static bool IsAutoPilot { get; private set; }

    [Header("Auto-Pilot Settings")]
    [SerializeField] private float _apKiteRadius = 4f;
    [SerializeField] private float _apCollectRadius = 6f;
    [SerializeField] private float _apDecisionInterval = 0.2f;
    [SerializeField] private float _apWanderChangeInterval = 3f;
    [SerializeField] private float _apLowHPThreshold = 0.4f;

    private float _apDecisionTimer;
    private Vector2 _apMoveDirection;
    private float _apWanderTimer;
    private Vector2 _apWanderDir;
    private readonly List<EnemyBase> _apNearbyEnemies = new List<EnemyBase>(32);
    private readonly Collider2D[] _apDropColliders = new Collider2D[64];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerStats>();
        _weaponManager = GetComponent<PlayerWeaponManager>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            IsAutoPilot = !IsAutoPilot;
            GameEvents.InvokeAutoPilotToggled(IsAutoPilot);
            DebugLogger.Log($"[AutoPilot] {(IsAutoPilot ? "ENABLED" : "DISABLED")}");
        }
    }

    private void FixedUpdate()
    {
        // Sync facing direction from character data (set by PlayerStats on init)
        if (GameManager.HasInstance && GameManager.Instance.SelectedCharacter != null)
            _faceRightByDefault = GameManager.Instance.SelectedCharacter.faceRightByDefault;

        Vector2 dir;
        if (IsAutoPilot && GameManager.HasInstance && GameManager.Instance.IsPlaying)
        {
            dir = ComputeAutoPilotDirection();
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            dir = new Vector2(h, v).normalized;
        }

        _rb.velocity = dir * _stats.MoveSpeed;

        // Clamp position to map boundaries (camera already clamps, player was missing)
        float mapHalf = MapManager.CurrentMapHalfSize;
        if (mapHalf > 0f)
        {
            var pos = _rb.position;
            pos.x = Mathf.Clamp(pos.x, -mapHalf, mapHalf);
            pos.y = Mathf.Clamp(pos.y, -mapHalf, mapHalf);
            _rb.position = pos;
        }

        bool isMoving = dir.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            _lastDirection = dir;

            if (_spriteRenderer != null && dir.x != 0f)
            {
                // Flip when moving opposite to the character's default facing direction
                _spriteRenderer.flipX = _faceRightByDefault ? dir.x < 0f : dir.x > 0f;
            }
        }
        else
        {
            // When idle, reset flipX only if the current animation state is idle
            // (idle frames are front-facing and should not be flipped)
            if (_animator != null && _spriteRenderer != null)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash != FrontRunHash)
                    _spriteRenderer.flipX = false;
            }
        }

        if (_animator != null)
        {
            _animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
        }

        if (_weaponManager != null)
        {
            _weaponManager.OnPlayerMoved(_rb.position, _lastDirection);
        }
    }

    // ── Auto-Pilot AI ─────────────────────────────────────────────

    private Vector2 ComputeAutoPilotDirection()
    {
        _apDecisionTimer -= Time.fixedDeltaTime;
        if (_apDecisionTimer > 0f)
            return _apMoveDirection;
        _apDecisionTimer = _apDecisionInterval;

        var playerPos = _rb.position;
        Vector2 bestDir = Vector2.zero;
        float urgency = 0f;

        // 1. Threat assessment — kite away from enemies when HP is low
        _apNearbyEnemies.Clear();
        SpatialGrid.QueryInRadius(playerPos, _apKiteRadius * 2f, _apNearbyEnemies);

        bool lowHP = _stats.CurrentHP < _stats.MaxHP * _apLowHPThreshold;

        if (_apNearbyEnemies.Count > 0)
        {
            Vector2 fleeDir = Vector2.zero;
            float closestDist = float.MaxValue;

            foreach (var enemy in _apNearbyEnemies)
            {
                if (enemy == null) continue;
                Vector2 toEnemy = (Vector2)enemy.transform.position - playerPos;
                float dist = toEnemy.magnitude;
                if (dist < closestDist) closestDist = dist;

                float weight = 1f / (dist * dist + 0.1f);
                fleeDir -= toEnemy.normalized * weight;
            }

            if (closestDist < _apKiteRadius && lowHP)
            {
                bestDir = fleeDir.normalized;
                urgency = (_apKiteRadius - closestDist) / _apKiteRadius;
            }
        }

        // 2. Collect drops — find nearby EXP/gold drops
        if (urgency < 0.7f)
        {
            Vector2 collectDir = Vector2.zero;
            bool foundDrops = false;

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                playerPos, _apCollectRadius, _apDropColliders,
                LayerMask.GetMask("Default"));

            for (int i = 0; i < hitCount; i++)
            {
                var drop = _apDropColliders[i].GetComponent<DropBase>();
                if (drop == null) continue;

                Vector2 toDrop = (Vector2)drop.transform.position - playerPos;
                float dist = toDrop.magnitude;

                float priority = 1f / (dist + 0.5f);
                collectDir += toDrop.normalized * priority;
                foundDrops = true;
            }

            if (foundDrops)
            {
                collectDir.Normalize();
                if (urgency > 0.1f && bestDir != Vector2.zero)
                    bestDir = Vector2.Lerp(collectDir, bestDir, urgency).normalized;
                else
                    bestDir = collectDir;
            }
        }

        // 3. Wander if no clear objective
        if (bestDir.sqrMagnitude < 0.01f)
        {
            _apWanderTimer -= _apDecisionInterval;
            if (_apWanderTimer <= 0f)
            {
                _apWanderDir = Random.insideUnitCircle.normalized;
                _apWanderTimer = _apWanderChangeInterval;
            }
            bestDir = _apWanderDir;
        }

        // 4. Stay away from map edges
        float mapHalf = MapManager.CurrentMapHalfSize;
        if (mapHalf > 0f)
        {
            float edgeBuffer = 8f;
            if (playerPos.x > mapHalf - edgeBuffer) bestDir.x -= 1f;
            if (playerPos.x < -mapHalf + edgeBuffer) bestDir.x += 1f;
            if (playerPos.y > mapHalf - edgeBuffer) bestDir.y -= 1f;
            if (playerPos.y < -mapHalf + edgeBuffer) bestDir.y += 1f;
        }

        _apMoveDirection = bestDir.normalized;
        return _apMoveDirection;
    }
}