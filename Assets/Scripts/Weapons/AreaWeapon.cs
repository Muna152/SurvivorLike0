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
    protected bool _followsPlayer = true;
    protected Vector2 _areaOrigin;

    public override void Initialize(WeaponData data, PlayerStats stats)
    {
        base.Initialize(data, stats);

        // Fallback: read zone prefab from WeaponData.projectilePrefab (same pattern as OrbitalWeapon)
        if (_areaPrefab == null && data != null && data.projectilePrefab != null)
        {
            _areaPrefab = data.projectilePrefab;
        }
    }

    protected override void Attack()
    {
        if (_followsPlayer)
        {
            // Auras persist — create once, then refresh stats/position each attack
            if (_currentArea == null)
                CreateAreaEffect();
            else
                RefreshAreaEffect();
        }
        else
        {
            // Non-following zones (e.g. Holy Water puddle): create only if no zone
            // currently exists. The zone lives for its duration and ticks damage at
            // _tickInterval independently. Once it expires, the next Attack() cycle
            // will spawn a fresh zone at the player's current position.
            if (_currentArea == null)
                CreateAreaEffect();
        }
    }

    protected virtual void CreateAreaEffect()
    {
        _areaOrigin = _playerPosition;

        if (_areaPrefab != null)
        {
            _currentArea = Instantiate(_areaPrefab, _areaOrigin, Quaternion.identity);
        }
        else
        {
            // Programmatic fallback: create a simple visual zone when no prefab is assigned
            _currentArea = CreateDefaultAreaVisual();
        }

        if (_currentArea == null) return;

        var ld = CurrentLevelData;
        if (ld != null)
        {
            _areaRadius = ld.area;
            _duration = ld.duration;
        }

        SetupAreaEffect();
        _tickTimer = _tickInterval;
    }

    protected virtual void RefreshAreaEffect()
    {
        if (_currentArea == null) return;

        if (_followsPlayer)
        {
            // Auras (e.g. Holy Light) persist as long as the weapon is active:
            // reset duration so the aura never expires, and keep it on the player.
            _currentArea.transform.position = _playerPosition;
            _areaOrigin = _playerPosition;

            var ld = CurrentLevelData;
            if (ld != null)
            {
                _duration = ld.duration;
                _areaRadius = ld.area;
            }
        }
        else
        {
            // Non-following zones (e.g. Holy Water puddle) have a fixed lifespan.
            // Do NOT reset duration — let the zone expire naturally so it can be
            // recreated at the player's current position on the next attack.
            var ld = CurrentLevelData;
            if (ld != null)
            {
                _areaRadius = ld.area;
            }
        }

        SetupAreaEffect();
    }

    protected virtual void SetupAreaEffect()
    {
        // Override in derived classes to setup specific area effect
    }

    /// <summary>
    /// Creates a default visual for the area zone when no prefab is available.
    /// </summary>
    protected virtual GameObject CreateDefaultAreaVisual()
    {
        var obj = new GameObject("AreaZone");
        obj.transform.position = _areaOrigin;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(_isHealing ? new Color(1f, 1f, 0.5f, 0.4f) : new Color(0.3f, 0.6f, 1f, 0.4f));
        sr.sortingOrder = 1;

        return obj;
    }

    protected override void Update()
    {
        base.Update();

        if (_currentArea != null)
        {
            // Move area with player only if it follows the player
            if (_followsPlayer)
            {
                _currentArea.transform.position = _playerPosition;
                _areaOrigin = _playerPosition;
            }

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

        Vector2 center = _followsPlayer ? _playerPosition : _areaOrigin;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _areaRadius);
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

        if (_currentArea != null && _followsPlayer)
        {
            _currentArea.transform.position = position;
            _areaOrigin = position;
        }
    }

    private void OnDestroy()
    {
        DestroyAreaEffect();
    }

    private static Sprite CreateCircleSprite(Color color)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                pixels[y * size + x] = (dx * dx + dy * dy < half * half) ? color : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
