using UnityEngine;

/// <summary>
/// Simple orthographic camera that smoothly follows the player.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float _lerpFactor = 0.1f;
    [SerializeField] private float _zOffset = -10f;

    private Transform _target;

    private void Start()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null) _target = player.transform;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, _zOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, _lerpFactor);
    }
}