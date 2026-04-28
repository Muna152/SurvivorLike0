using UnityEngine;

/// <summary>
/// Holy Water weapon: Creates a damaging pool on the ground.
/// </summary>
public class HolyWater : AreaWeapon
{
    private void Awake()
    {
        _isHealing = false;
    }

    protected override void CreateAreaEffect()
    {
        if (_areaPrefab == null) return;

        _currentArea = Instantiate(_areaPrefab, _playerPosition, Quaternion.identity);

        var ld = CurrentLevelData;
        if (ld != null)
        {
            _areaRadius = 2.5f;
            _duration = 5f;
        }

        SetupAreaEffect();
        _tickTimer = _tickInterval;
    }

    protected override void SetupAreaEffect()
    {
        if (_currentArea != null)
        {
            var sr = _currentArea.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var ld = CurrentLevelData;
                if (ld != null)
                {
                    sr.size = new Vector2(_areaRadius * 2f, _areaRadius * 2f);
                }
            }
        }
    }

    public override void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        base.OnPlayerMoved(position, direction);

        // Holy water stays where it was created (doesn't follow player)
    }
}
