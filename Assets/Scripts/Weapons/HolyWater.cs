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
}
