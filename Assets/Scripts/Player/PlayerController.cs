using UnityEngine;

/// <summary>
/// Top-down 2D movement via WASD / arrow keys.
/// Uses Rigidbody2D.velocity. Broadcasts position/direction to weapon manager.
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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerStats>();
        _weaponManager = GetComponent<PlayerWeaponManager>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        // Sync facing direction from character data (set by PlayerStats on init)
        if (GameManager.HasInstance && GameManager.Instance.SelectedCharacter != null)
            _faceRightByDefault = GameManager.Instance.SelectedCharacter.faceRightByDefault;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        var dir = new Vector2(h, v).normalized;

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
}