using UnityEngine;

/// <summary>
/// Area weapon that creates a zone effect (damage or healing) in a specific area.
/// Examples: Holy Light (healing zone), Holy Water (damage zone).
/// </summary>
public abstract class AreaWeapon : WeaponBase
{
    [SerializeField] protected GameObject _areaPrefab;
    protected GameObject _currentArea;
    protected float _areaRadius;
    protected float _duration;
    protected float _tickInterval = 0.5f;
    protected float _tickTimer;
    protected bool _isHealing;

    protected override void Attack()
    {
        // Create or refresh the area effect
        if (_currentArea == null)
        {
            CreateAreaEffect();
        }
        else
        {
            RefreshAreaEffect();
        }
    }

    protected virtual void CreateAreaEffect()
    {
        if (_areaPrefab == null) return;

        _currentArea = Instantiate(_areaPrefab, _playerPosition, Quaternion.identity);
        
        var ld = CurrentLevelData;
        if (ld != null)
        {
            _areaRadius = 3f;
            _duration = 5f;
        }
        
        SetupAreaEffect();
        _tickTimer = _tickInterval;
    }

    protected virtual void RefreshAreaEffect()
    {
        if (_currentArea != null)
        {
            _currentArea.transform.position = _playerPosition;
        }
        
        var ld = CurrentLevelData;
        if (ld != null)
        {
            _duration = 5f;
        }
        
        SetupAreaEffect();
    }

    protected virtual void SetupAreaEffect()
    {
        // Override in derived classes to setup specific area effect
    }

    protected override void Update()
    {
        base.Update();
        
        if (_currentArea != null)
        {
            // Update area position to follow player
            _currentArea.transform.position = _playerPosition;
            
            // Tick damage/healing
            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                ApplyAreaEffect();
                _tickTimer = _tickInterval;
            }
            
            // Check duration
            _duration -= Time.deltaTime;
            if (_duration <= 0f)
            {
                DestroyAreaEffect();
            }
        }
    }

    protected virtual void ApplyAreaEffect()
    {
        if (_isHealing)
        {
            ApplyHealing();
        }
        else
        {
            ApplyDamage();
        }
    }

    protected virtual void ApplyDamage()
    {
        var ld = CurrentLevelData;
        if (ld == null || _playerStats == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(_playerPosition, _areaRadius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(ld.damage * _playerStats.DamageMultiplier));
            }
        }
    }

    protected virtual void ApplyHealing()
    {
        var ld = CurrentLevelData;
        if (ld == null || _playerStats == null) return;

        // Heal player
        int healAmount = Mathf.RoundToInt(ld.damage * _playerStats.DamageMultiplier);
        _playerStats.Heal(healAmount);
    }

    protected virtual void DestroyAreaEffect()
    {
        if (_currentArea != null)
        {
            Destroy(_currentArea);
            _currentArea = null;
        }
    }

    public override void OnPlayerMoved(Vector2 position, Vector2 direction)
    {
        base.OnPlayerMoved(position, direction);

        // Area effect follows player
        if (_currentArea != null)
        {
            _currentArea.transform.position = position;
        }
    }

    private void OnDestroy()
    {
        DestroyAreaEffect();
    }
}
