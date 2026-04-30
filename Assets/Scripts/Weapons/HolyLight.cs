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
                float diameter = _areaRadius * 2f;
                float spriteWorldSize = sr.sprite.bounds.size.x;
                float scale = diameter / spriteWorldSize;
                _currentArea.transform.localScale = new Vector3(scale, scale, 1f);
                sr.sortingOrder = 1;
            }
        }
    }
}
