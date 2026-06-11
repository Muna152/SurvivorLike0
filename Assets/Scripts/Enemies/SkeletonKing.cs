using UnityEngine;

/// <summary>
/// Skeleton King BOSS: Appears at 10 minutes. HP=500.
/// Phase 1: Summons skeleton minions + chases player.
/// Phase 2: Adds shockwave attack + faster summons.
/// </summary>
public class SkeletonKing : BossEnemy
{
    [Header("Skeleton King Config")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private GameObject _shockwavePrefab;
    [SerializeField] private string summonEnemyId = "skeleton";
    [SerializeField] private int _summonCount = 4;
    [SerializeField] private float _shockwaveDamage = 3f;
    [SerializeField] private float _shockwaveMaxRadius = 5f;
    [SerializeField] private float _shockwaveSpeed = 6f;

    private int _attackPattern; // Rotates between attack types
    private bool _shockwavePoolRegistered;
    private string _shockwavePoolKey;
    private DifficultyManager _difficultyManager; // Cached to avoid repeated Singleton lock

    public override void Initialize(EnemyData data)
    {
        // Override boss defaults
        _phaseThresholds = new float[] { 0.5f }; // One phase transition at 50% HP
        _expMultiplier = 50;
        _goldMultiplier = 20;
        _attackInterval = 3f;
        _isUnkillable = false;

        base.Initialize(data);
        _attackPattern = 0;
        _difficultyManager = DifficultyManager.HasInstance ? DifficultyManager.Instance : null;
        RegisterShockwavePool();
    }

    private void RegisterShockwavePool()
    {
        if (_shockwavePoolRegistered || _shockwavePrefab == null) return;

        _shockwavePoolKey = _shockwavePrefab.name.Replace("(Clone)", "").Trim();

        if (PoolManager.HasInstance && PoolManager.Instance.HasPool(_shockwavePoolKey))
        {
            _shockwavePoolRegistered = true;
            return;
        }

        var prefab = _shockwavePrefab;
        PoolManager.Instance.Register<BossShockwave>(
            _shockwavePoolKey,
            () =>
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<BossShockwave>();
            },
            sw => { sw.ResetForReuse(); sw.gameObject.SetActive(false); },
            prewarmCount: 8,
            maxSize: 20
        );

        _shockwavePoolRegistered = true;
    }

    public override void OnFixedTick(float dt)
    {
        if (_phaseTransitioning) return;

        // Bosses chase slower but persistently
        var player = GetPlayerController();
        if (player == null) return;

        float speedMultiplier = _currentPhase >= 1 ? 1.3f : 1f;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * speedMultiplier * dt);
    }

    protected override void ExecuteAttack()
    {
        if (_phaseTransitioning) return;

        switch (_attackPattern % GetAttackCount())
        {
            case 0:
                SummonSkeletons();
                break;
            case 1:
                if (_currentPhase >= 1)
                    ShockwaveAttack();
                else
                    SummonSkeletons();
                break;
        }
        _attackPattern++;
    }

    private int GetAttackCount()
    {
        return _currentPhase >= 1 ? 2 : 1;
    }

    /// <summary>Summon skeleton minions around the boss.</summary>
    private void SummonSkeletons()
    {
        var summonData = EnemyDatabase.Instance != null ? EnemyDatabase.Instance.GetById(summonEnemyId) : null;
        if (summonData == null || summonData.prefab == null) return;

        int count = _currentPhase >= 1 ? _summonCount + 2 : _summonCount;
        var player = GetPlayerController();

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            // Check enemy cap
            if (EnemyBase.ActiveEnemyCount >= (GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.maxEnemiesOnScreen - 10 : 190)) break;

            var enemyObj = PoolManager.Instance.Get<EnemyBase>(summonData.enemyName);
            if (enemyObj != null)
            {
                enemyObj.transform.position = spawnPos;
                enemyObj.Initialize(summonData);
            }
        }
    }

    /// <summary>Get a pooled BossShockwave. Returns null if pool is exhausted.</summary>
    private void ShockwaveAttack()
    {
        if (_shockwavePrefab == null) return;

        BossShockwave sw = null;
        if (_shockwavePoolRegistered && PoolManager.HasInstance)
        {
            sw = PoolManager.Instance.Get<BossShockwave>(_shockwavePoolKey);
        }

        if (sw != null)
        {
            sw.transform.position = transform.position;
            sw.transform.rotation = Quaternion.identity;
            sw.SetPoolKey(_shockwavePoolKey);
        }

        if (sw != null)
        {
            float dmgScale = _difficultyManager != null ? _difficultyManager.DamageMultiplier : 1f;
            sw.Initialize(_shockwaveDamage * dmgScale, _shockwaveSpeed, _shockwaveMaxRadius);
        }
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);

        // Phase 2: Summon extra minions on phase transition
        if (newPhase >= 1)
        {
            SummonSkeletons();
        }
    }
}
