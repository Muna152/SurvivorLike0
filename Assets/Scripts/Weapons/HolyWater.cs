using UnityEngine;

/// <summary>
/// Holy Water weapon: Creates a damaging pool on the ground.
/// The pool stays where it was created and does NOT follow the player.
/// </summary>
public class HolyWater : AreaWeapon
{
    private void Awake()
    {
        _isHealing = false;
        _followsPlayer = false;
    }

    protected override void SetupAreaEffect()
    {
        if (_currentArea != null)
        {
            var sr = _currentArea.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.size = new Vector2(_areaRadius * 2f, _areaRadius * 2f);
                sr.sortingOrder = 1;
            }
        }
    }
}
