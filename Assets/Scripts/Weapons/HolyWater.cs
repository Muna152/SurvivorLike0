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
                // Scale the zone so its visual diameter matches _areaRadius * 2.
                // Assumes the sprite's native size is 1 world-unit wide (PixelsPerUnit = sprite width).
                float diameter = _areaRadius * 2f;
                float spriteWorldSize = sr.sprite.bounds.size.x;
                float scale = diameter / spriteWorldSize;
                _currentArea.transform.localScale = new Vector3(scale, scale, 1f);
                sr.sortingOrder = 1;
            }
        }
    }
}
