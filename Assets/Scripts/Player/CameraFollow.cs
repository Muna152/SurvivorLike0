using UnityEngine;

/// <summary>
/// Orthographic camera that smoothly follows the player, clamped within map bounds.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float _lerpFactor = 0.15f;
    [SerializeField] private float _zOffset = -10f;
    [SerializeField] private float _mapHalfSize = 100f;

    private Transform _target;
    private Camera _cam;
    private float _halfCamHeight;
    private float _halfCamWidth;
    private Vector3 _targetPos; // Reusable to avoid allocation in LateUpdate

    private void Start()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null) _target = player.transform;

        _cam = GetComponent<Camera>();
        if (_cam != null && _cam.orthographic)
        {
            _halfCamHeight = _cam.orthographicSize;
            _halfCamWidth = _halfCamHeight * _cam.aspect;
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        _targetPos.x = _target.position.x;
        _targetPos.y = _target.position.y;
        _targetPos.z = _zOffset;

        // Clamp camera within map boundaries
        if (_cam != null && _cam.orthographic)
        {
            float minX = -_mapHalfSize + _halfCamWidth;
            float maxX = _mapHalfSize - _halfCamWidth;
            float minY = -_mapHalfSize + _halfCamHeight;
            float maxY = _mapHalfSize - _halfCamHeight;

            _targetPos.x = Mathf.Clamp(_targetPos.x, minX, maxX);
            _targetPos.y = Mathf.Clamp(_targetPos.y, minY, maxY);
        }

        transform.position = Vector3.Lerp(transform.position, _targetPos, _lerpFactor);
    }
}