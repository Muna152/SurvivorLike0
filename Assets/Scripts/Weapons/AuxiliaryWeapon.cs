using UnityEngine;

/// <summary>
/// Auxiliary weapon that provides buffs/effects without directly damaging enemies.
/// Examples: Increased pickup range, experience bonus, etc.
/// </summary>
public abstract class AuxiliaryWeapon : WeaponBase
{
    protected float _effectDuration;
    protected float _effectTimer;
    protected bool _effectActive;

    protected override void Attack()
    {
        // Activate or refresh the auxiliary effect
        ActivateEffect();
    }

    protected virtual void ActivateEffect()
    {
        var ld = CurrentLevelData;
        if (ld != null)
        {
            _effectDuration = 10f;
        }
        
        _effectTimer = _effectDuration;
        _effectActive = true;
        
        OnEffectActivated();
    }

    protected virtual void OnEffectActivated()
    {
        // Override in derived classes to apply specific effects
    }

    protected virtual void OnEffectDeactivated()
    {
        // Override in derived classes to remove specific effects
    }

    protected override void Update()
    {
        base.Update();
        
        if (_effectActive)
        {
            _effectTimer -= Time.deltaTime;
            
            if (_effectTimer <= 0f)
            {
                _effectActive = false;
                OnEffectDeactivated();
            }
        }
    }

    protected virtual void ApplyPassiveEffect()
    {
        // Override in derived classes for passive (always-on) effects
    }

    public override void Initialize(WeaponData data, PlayerStats stats)
    {
        base.Initialize(data, stats);
        
        // Apply passive effects immediately
        ApplyPassiveEffect();
    }

    public override void Upgrade()
    {
        base.Upgrade();
        
        // Refresh passive effects on upgrade
        ApplyPassiveEffect();
    }
}
