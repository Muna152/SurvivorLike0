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
    private Vector2 _lastDirection = Vector2.down;

    public Vector2 LastDirection => _lastDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerStats>();
        _weaponManager = GetComponent<PlayerWeaponManager>();
    }

    private void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        var dir = new Vector2(h, v).normalized;

        _rb.velocity = dir * _stats.MoveSpeed;

        if (dir.sqrMagnitude > 0.01f)
        {
            _lastDirection = dir;
        }

        if (_weaponManager != null)
        {
            _weaponManager.OnPlayerMoved(_rb.position, _lastDirection);
        }
    }
}