using UnityEngine;

/// <summary>
/// Holy Light weapon: Creates a healing aura around the player.
/// </summary>
public class HolyLight : AreaWeapon
{
    private void Awake()
    {
        _isHealing = true;
        _followsPlayer = true;
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
