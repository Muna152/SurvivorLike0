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
}
