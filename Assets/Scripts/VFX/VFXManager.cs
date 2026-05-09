using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all visual effects: pooling, spawning, and GameEvents-driven auto-triggering.
/// Follows the AudioManager pattern — subscribes to GameEvents for automatic VFX playback.
/// Loads VFX prefabs from Resources/VFX at runtime.
/// </summary>
public class VFXManager : Singleton<VFXManager>
{
    // ── Prefab references (loaded in Awake from Resources) ────
    private GameObject _hitEffectPrefab;
    private GameObject _enemyDeathPrefab;
    private GameObject _expPickupPrefab;
    private GameObject _levelUpPrefab;
    private GameObject _evolvePrefab;
    private GameObject _slashVFXPrefab;
    private GameObject _trailVFXPrefab;
    private GameObject _explosionVFXPrefab;
    private GameObject _glowVFXPrefab;
    private GameObject _splashVFXPrefab;
    private GameObject _impactVFXPrefab;
    private GameObject _damageNumberPrefab;

    // ── Settings ──────────────────────────────────────────────
    private const int HitEffectPoolSize = 10;
    private const int EnemyDeathPoolSize = 15;
    private const int ExpPickupPoolSize = 15;
    private const int LevelUpPoolSize = 5;
    private const int EvolvePoolSize = 5;
    private const int DamageNumberPoolSize = 20;
    private const int WeaponVFXPoolSize = 8;

    // ── Internal ──────────────────────────────────────────────
    private bool _poolsRegistered;
    private bool _damageNumberPoolRegistered;
    private PlayerController _cachedPlayer;

    // ── Lifecycle ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        LoadPrefabs();
        RegisterPools();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnDropCollected += HandleDropCollected;
        GameEvents.OnPlayerLevelUp += HandlePlayerLevelUp;
        GameEvents.OnWeaponEvolved += HandleWeaponEvolved;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnDropCollected -= HandleDropCollected;
        GameEvents.OnPlayerLevelUp -= HandlePlayerLevelUp;
        GameEvents.OnWeaponEvolved -= HandleWeaponEvolved;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cachedPlayer = null;
        _poolsRegistered = false;
        _damageNumberPoolRegistered = false;
        RegisterPools();
    }

    // ── Prefab Loading ────────────────────────────────────────

    private void LoadPrefabs()
    {
        _hitEffectPrefab = Resources.Load<GameObject>("VFX/HitEffect");
        _enemyDeathPrefab = Resources.Load<GameObject>("VFX/EnemyDeath");
        _expPickupPrefab = Resources.Load<GameObject>("VFX/ExpPickup");
        _levelUpPrefab = Resources.Load<GameObject>("VFX/LevelUp");
        _evolvePrefab = Resources.Load<GameObject>("VFX/EvolveEffect");
        _slashVFXPrefab = Resources.Load<GameObject>("VFX/SlashVFX");
        _trailVFXPrefab = Resources.Load<GameObject>("VFX/TrailVFX");
        _explosionVFXPrefab = Resources.Load<GameObject>("VFX/ExplosionVFX");
        _glowVFXPrefab = Resources.Load<GameObject>("VFX/GlowVFX");
        _splashVFXPrefab = Resources.Load<GameObject>("VFX/SplashVFX");
        _impactVFXPrefab = Resources.Load<GameObject>("VFX/ImpactVFX");
        _damageNumberPrefab = Resources.Load<GameObject>("VFX/DamageNumber");
    }

    // ── Pool Registration ────────────────────────────────────

    private void RegisterPools()
    {
        if (_poolsRegistered) return;
        var pm = PoolManager.Instance;
        if (pm == null) return;

        RegisterVFXPool(pm, "HitEffect", _hitEffectPrefab, HitEffectPoolSize);
        RegisterVFXPool(pm, "EnemyDeath", _enemyDeathPrefab, EnemyDeathPoolSize);
        RegisterVFXPool(pm, "ExpPickup", _expPickupPrefab, ExpPickupPoolSize);
        RegisterVFXPool(pm, "LevelUp", _levelUpPrefab, LevelUpPoolSize);
        RegisterVFXPool(pm, "EvolveEffect", _evolvePrefab, EvolvePoolSize);

        // Weapon VFX
        RegisterVFXPool(pm, "SlashVFX", _slashVFXPrefab, WeaponVFXPoolSize);
        RegisterVFXPool(pm, "TrailVFX", _trailVFXPrefab, WeaponVFXPoolSize);
        RegisterVFXPool(pm, "ExplosionVFX", _explosionVFXPrefab, WeaponVFXPoolSize);
        RegisterVFXPool(pm, "GlowVFX", _glowVFXPrefab, WeaponVFXPoolSize);
        RegisterVFXPool(pm, "SplashVFX", _splashVFXPrefab, WeaponVFXPoolSize);
        RegisterVFXPool(pm, "ImpactVFX", _impactVFXPrefab, WeaponVFXPoolSize);

        _poolsRegistered = true;
    }

    private void RegisterVFXPool(PoolManager pm, string key, GameObject prefab, int count)
    {
        if (prefab == null) return;
        pm.Register<VFXBase>(key,
            () =>
            {
                var obj = Instantiate(prefab, transform);
                obj.name = key;
                return obj.GetComponent<VFXBase>();
            },
            vfx => vfx.ResetForReuse(),
            count);
    }

    private void EnsureDamageNumberPool()
    {
        if (_damageNumberPoolRegistered) return;
        if (_damageNumberPrefab == null) return;
        var pm = PoolManager.Instance;
        if (pm == null) return;

        pm.Register<DamageNumber>("DamageNumber",
            () =>
            {
                var obj = Instantiate(_damageNumberPrefab, transform);
                obj.name = "DamageNumber";
                return obj.GetComponent<DamageNumber>();
            },
            dn => dn.ResetForReuse(),
            DamageNumberPoolSize);
        _damageNumberPoolRegistered = true;
    }

    // ── Public Spawn API ──────────────────────────────────────

    public void PlayHitEffect(Vector3 position) => SpawnVFX("HitEffect", position);
    public void PlayEnemyDeath(Vector3 position) => SpawnVFX("EnemyDeath", position);
    public void PlayExpPickup(Vector3 position) => SpawnVFX("ExpPickup", position);
    public void PlayLevelUp(Vector3 position) => SpawnVFX("LevelUp", position);
    public void PlayEvolveEffect(Vector3 position) => SpawnVFX("EvolveEffect", position);

    public void PlayWeaponVFX(WeaponData.VFXType vfxType, Vector3 position)
    {
        string key = VFXTypeToPoolKey(vfxType);
        if (!string.IsNullOrEmpty(key))
            SpawnVFX(key, position);
    }

    public void PlayDamageNumber(Vector3 position, int amount, Color color)
    {
        if (_damageNumberPrefab == null) return;
        EnsureDamageNumberPool();

        var dn = PoolManager.Instance.Get<DamageNumber>("DamageNumber");
        if (dn != null)
            dn.Show(position, amount, color);
    }

    // ── Event Handlers ────────────────────────────────────────

    private void HandlePlayerDamaged(int damage)
    {
        var player = GetCachedPlayer();
        if (player != null)
        {
            PlayHitEffect(player.transform.position);
            PlayDamageNumber(player.transform.position, damage, new Color(1f, 0.3f, 0.3f));
        }
    }

    private void HandleEnemyHit(EnemyBase _) { }

    private void HandleEnemyDamaged(EnemyBase enemy, int damage)
    {
        if (enemy == null) return;
        PlayDamageNumber(enemy.transform.position, damage, Color.white);
    }

    private void HandleEnemyDied(EnemyBase enemy)
    {
        if (enemy == null) return;
        PlayEnemyDeath(enemy.transform.position);
    }

    private void HandleDropCollected(DropBase drop)
    {
        if (drop == null) return;
        PlayExpPickup(drop.transform.position);
    }

    private void HandlePlayerLevelUp(int _)
    {
        var player = GetCachedPlayer();
        if (player != null)
            PlayLevelUp(player.transform.position);
    }

    private void HandleWeaponEvolved(WeaponBase weapon)
    {
        if (weapon == null) return;
        PlayEvolveEffect(weapon.transform.position);
    }

    // ── Helpers ────────────────────────────────────────────────

    private void SpawnVFX(string poolKey, Vector3 position)
    {
        if (!PoolManager.HasInstance) return;
        if (!PoolManager.Instance.HasPool(poolKey)) return;

        var vfx = PoolManager.Instance.Get<VFXBase>(poolKey);
        if (vfx != null)
            vfx.Play(position);
    }

    private string VFXTypeToPoolKey(WeaponData.VFXType vfxType)
    {
        switch (vfxType)
        {
            case WeaponData.VFXType.Slash: return "SlashVFX";
            case WeaponData.VFXType.Trail: return "TrailVFX";
            case WeaponData.VFXType.Explosion: return "ExplosionVFX";
            case WeaponData.VFXType.Glow: return "GlowVFX";
            case WeaponData.VFXType.Splash: return "SplashVFX";
            case WeaponData.VFXType.Impact: return "ImpactVFX";
            default: return null;
        }
    }

    private PlayerController GetCachedPlayer()
    {
        if (_cachedPlayer == null)
            _cachedPlayer = FindObjectOfType<PlayerController>();
        return _cachedPlayer;
    }
}
