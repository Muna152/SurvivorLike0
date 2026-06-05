using System.Collections.Generic;
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
    protected float _tickInterval = -1f; // -1 = not yet initialized from config
    protected float _tickTimer;
    protected bool _isHealing;
    protected bool _followsPlayer = true;
    protected Vector2 _areaOrigin;

    // Pooling support
    private bool _areaPoolRegistered;
    private string _areaPoolKey;

    /// <summary>Tick interval — lazily loaded from GameBalanceConfig if not overridden.</summary>
    protected float TickInterval
    {
        get
        {
            if (_tickInterval < 0f)
                _tickInterval = GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.areaWeaponTickInterval : 0.5f;
            return _tickInterval;
        }
    }

    public override void Initialize(WeaponData data, PlayerStats stats)
    {
        base.Initialize(data, stats);

        // Fallback: read zone prefab from WeaponData.projectilePrefab (same pattern as OrbitalWeapon)
        if (_areaPrefab == null && data != null && data.projectilePrefab != null)
        {
            _areaPrefab = data.projectilePrefab;
        }

        RegisterAreaPool();
    }

    private void RegisterAreaPool()
    {
        if (_areaPoolRegistered || _areaPrefab == null) return;

        _areaPoolKey = _areaPrefab.name.Replace("(Clone)", "").Trim();

        if (PoolManager.HasInstance && PoolManager.Instance.HasPool(_areaPoolKey))
        {
            _areaPoolRegistered = true;
            return;
        }

        var prefab = _areaPrefab;
        PoolManager.Instance.Register<Transform>(
            _areaPoolKey,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.transform;
            },
            t => t.gameObject.SetActive(false),
            prewarmCount: 2,
            maxSize: 5
        );

        _areaPoolRegistered = true;
    }

    /// <summary>Get a pooled area zone, or Instantiate as fallback.</summary>
    private GameObject GetPooledArea(Vector2 position)
    {
        GameObject area = null;
        if (_areaPoolRegistered && PoolManager.HasInstance)
        {
            var t = PoolManager.Instance.Get<Transform>(_areaPoolKey);
            if (t != null) area = t.gameObject;
        }

        if (area == null)
        {
            // Fallback: instantiate directly
            area = Instantiate(_areaPrefab, position, Quaternion.identity);
        }
        else
        {
            area.transform.position = position;
            area.transform.rotation = Quaternion.identity;
            area.transform.localScale = Vector3.one;
        }

        return area;
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
            _currentArea = GetPooledArea(_areaOrigin);
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
        _tickTimer = TickInterval;
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
        // Default: scale the area visual so the visible circle matches _areaRadius
        ScaleAreaVisual();
    }

    /// <summary>
    /// Small buffer added to the data-driven radius so that enemies visually
    /// inside the zone are always within damage range, even if the sprite has
    /// slight transparent padding or the visual scale is fractionally off.
    /// </summary>
    private const float DamageRadiusBuffer = 0.15f;

    /// <summary>
    /// Returns the damage radius: data-driven _areaRadius + buffer.
    /// </summary>
    protected float GetDamageRadius() => _areaRadius + DamageRadiusBuffer;

    /// <summary>
    /// Scales the area GameObject so the sprite's visible circle radius matches _areaRadius.
    /// Accounts for transparent padding in the sprite texture.
    /// </summary>
    protected void ScaleAreaVisual()
    {
        if (_currentArea == null) return;
        var sr = _currentArea.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float fillRatio = GetSpriteFillRatio(sr.sprite);
        float diameter = _areaRadius * 2f;
        float spriteVisualSize = sr.sprite.bounds.size.x * fillRatio;
        float scale = diameter / spriteVisualSize;
        _currentArea.transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// Measures the ratio of the sprite's visible circle diameter to its bounds diameter.
    /// Samples the texture alpha to find the actual opaque extent.
    /// Result is cached per sprite to avoid repeated computation.
    /// </summary>
    private static float GetSpriteFillRatio(Sprite sprite)
    {
        int id = sprite.GetInstanceID();
        if (_spriteFillCache.TryGetValue(id, out float ratio))
            return ratio;

        var tex = sprite.texture;
        if (tex == null || !tex.isReadable)
        {
            // Texture not readable — assume circle fills bounds
            ratio = 1f;
        }
        else
        {
            // Find max distance from center where alpha > threshold
            int cx = tex.width / 2;
            int cy = tex.height / 2;
            int maxDist = 0;
            int threshold = 30; // alpha threshold for "visible"
            var pixels = tex.GetPixels();

            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    if (pixels[y * tex.width + x].a * 255f > threshold)
                    {
                        int dx = x - cx;
                        int dy = y - cy;
                        int dist = dx * dx + dy * dy;
                        if (dist > maxDist) maxDist = dist;
                    }
                }
            }

            float visibleRadius = Mathf.Sqrt(maxDist);
            float boundsRadius = tex.width * 0.5f;
            ratio = boundsRadius > 0f ? visibleRadius / boundsRadius : 1f;
        }

        _spriteFillCache[id] = ratio;
        return ratio;
    }

    /// <summary>
    /// Creates a default visual for the area zone when no prefab is available.
    /// </summary>
    protected virtual GameObject CreateDefaultAreaVisual()
    {
        var obj = new GameObject("AreaZone");
        obj.transform.position = _areaOrigin;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedCircleSprite(_isHealing);
        sr.sortingLayerName = "VFX";

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
                _tickTimer = TickInterval;
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

    // Reusable list for QueryInRadius — avoids GC and shared-list corruption
    private static readonly List<EnemyBase> _damageResults = new List<EnemyBase>(32);

    protected virtual void ApplyDamage()
    {
        var ld = CurrentLevelData;
        if (ld == null || _playerStats == null) return;

        // Use data-driven damage radius (never read from visual bounds)
        Vector2 center = _currentArea != null ? (Vector2)_currentArea.transform.position : _areaOrigin;
        float radius = GetDamageRadius();
        SpatialGrid.QueryInRadius(center, radius, _damageResults);

#if UNITY_EDITOR
        // Record hit results for Gizmo comparison
        _lastHitInstanceIDs.Clear();
        for (int i = 0; i < _damageResults.Count; i++)
            _lastHitInstanceIDs.Add(_damageResults[i].GetInstanceID());
        _lastTickCenter = center;
        _lastTickRadius = radius;
        _hasLastTick = true;
#endif

        for (int i = 0; i < _damageResults.Count; i++)
        {
            _damageResults[i].TakeDamage(Mathf.RoundToInt(ld.damage * _playerStats.DamageMultiplier));
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
            if (_areaPoolRegistered && !string.IsNullOrEmpty(_areaPoolKey) && PoolManager.HasInstance && PoolManager.Instance.HasPool(_areaPoolKey))
            {
                PoolManager.Instance.Return(_areaPoolKey, _currentArea.transform);
            }
            else
            {
                Destroy(_currentArea);
            }
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

#if UNITY_EDITOR
    // ── Debug: track last tick results for Gizmo comparison ──
    private static readonly HashSet<int> _lastHitInstanceIDs = new HashSet<int>();
    private Vector2 _lastTickCenter;
    private float _lastTickRadius;
    private bool _hasLastTick;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Always draw damage circle if zone exists
        if (_currentArea != null)
        {
            var center = (Vector2)_currentArea.transform.position;
            float r = GetDamageRadius();
            DrawCircleGizmo(center, r, new Color(1f, 0.3f, 0.3f, 0.8f), 2f);

            // White thin circle = raw _areaRadius (no buffer)
            DrawCircleGizmo(center, _areaRadius, new Color(1f, 1f, 1f, 0.3f), 1f);
        }

        if (!_hasLastTick) return;

        // Brute-force scan: find ALL active enemies within the last tick radius
        var bruteForce = new HashSet<int>();
        float radiusSq = _lastTickRadius * _lastTickRadius;
        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e == null) continue;
            if ((_lastTickCenter - (Vector2)e.transform.position).sqrMagnitude <= radiusSq)
                bruteForce.Add(e.GetInstanceID());
        }

        // Missed enemies: in brute-force but NOT in last SpatialGrid result
        var missed = new List<EnemyBase>();
        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e == null) continue;
            if (bruteForce.Contains(e.GetInstanceID()) && !_lastHitInstanceIDs.Contains(e.GetInstanceID()))
                missed.Add(e);
        }

        // Log missed enemies for diagnosis (disabled — bug fixed)
        // if (missed.Count > 0)
        // {
        //     foreach (var e in missed)
        //     {
        //         SpatialGrid.CellCoord(e.transform.position, out int actualCx, out int actualCy);
        //         long expectedKey = SpatialGrid.CellKey(actualCx, actualCy);
        //         long registeredKey = e.LastCellKey;
        //         bool isRegistered = SpatialGrid.IsRegistered(e);
        //         UnityEngine.Debug.LogWarning(
        //             $"[AreaWeapon] MISSED enemy '{e.name}' pos={e.transform.position:F2} " +
        //             $"registeredCell={registeredKey} expectedCell={expectedKey} " +
        //             $"isRegistered={isRegistered} " +
        //             $"center={_lastTickCenter:F2} radius={_lastTickRadius:F2}");
        //     }
        // }

        // Draw missed enemies as bright magenta spheres with connecting line to center
        if (missed.Count > 0)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 1f); // bright magenta
            foreach (var e in missed)
            {
                Gizmos.DrawSphere(e.transform.position, 0.35f);
                Gizmos.DrawLine(_lastTickCenter, e.transform.position);
            }
        }

        // Draw SpatialGrid scanned cells (from last query)
        SpatialGrid.DrawDebugGizmos();

        // HUD text: show hit count vs brute-force count
        var style = new GUIStyle();
        style.richText = true;
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        UnityEditor.Handles.Label(
            _lastTickCenter + Vector2.up * (_lastTickRadius + 0.5f),
            $"<color=red>Grid:{_lastHitInstanceIDs.Count}</color> / <color=cyan>Brute:{bruteForce.Count}</color> / <color=magenta>Missed:{missed.Count}</color>",
            style
        );
    }

    private static void DrawCircleGizmo(Vector2 center, float radius, Color color, float lineWidth = 1f)
    {
        Gizmos.color = color;
        const int seg = 48;
        for (int i = 0; i < seg; i++)
        {
            float a1 = (i / (float)seg) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)seg) * Mathf.PI * 2f;
            var p1 = new Vector3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, 0f);
            var p2 = new Vector3(center.x + Mathf.Cos(a2) * radius, center.y + Mathf.Sin(a2) * radius, 0f);
            Gizmos.DrawLine(p1, p2);
        }
    }
#endif

    // Cache sprite fill ratios: key = sprite instanceID, value = visibleRadius / boundsRadius
    private static readonly Dictionary<int, float> _spriteFillCache = new Dictionary<int, float>(4);

    private static Sprite _cachedHealSprite;
    private static Sprite _cachedDamageSprite;
    private static Texture2D _cachedHealTex;
    private static Texture2D _cachedDamageTex;

    /// <summary>Clear all static caches and destroy cached textures. Call on session end.</summary>
    public static void ClearStaticCache()
    {
        _spriteFillCache.Clear();

        if (_cachedHealSprite != null) { DestroyHelper.Destroy(_cachedHealSprite); _cachedHealSprite = null; }
        if (_cachedDamageSprite != null) { DestroyHelper.Destroy(_cachedDamageSprite); _cachedDamageSprite = null; }
        if (_cachedHealTex != null) { DestroyHelper.Destroy(_cachedHealTex); _cachedHealTex = null; }
        if (_cachedDamageTex != null) { DestroyHelper.Destroy(_cachedDamageTex); _cachedDamageTex = null; }
    }

    private static Sprite GetCachedCircleSprite(bool isHealing)
    {
        if (isHealing)
        {
            if (_cachedHealSprite == null)
            {
                var result = CreateCircleSprite(new Color(1f, 1f, 0.5f, 0.4f));
                _cachedHealSprite = result.sprite;
                _cachedHealTex = result.texture;
            }
            return _cachedHealSprite;
        }
        else
        {
            if (_cachedDamageSprite == null)
            {
                var result = CreateCircleSprite(new Color(0.3f, 0.6f, 1f, 0.4f));
                _cachedDamageSprite = result.sprite;
                _cachedDamageTex = result.texture;
            }
            return _cachedDamageSprite;
        }
    }

    private struct CircleSpriteResult { public Sprite sprite; public Texture2D texture; }

    private static CircleSpriteResult CreateCircleSprite(Color color)
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
        return new CircleSpriteResult
        {
            sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size),
            texture = tex
        };
    }
}
