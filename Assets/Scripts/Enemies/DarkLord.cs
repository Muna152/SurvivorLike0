using UnityEngine;

/// <summary>
/// Dark Lord BOSS: Appears at 20 minutes. HP=2000.
/// Phase 1: Fires fan-shaped projectiles at player.
/// Phase 2: Adds teleport + ring bullet patterns.
/// Phase 3: Summons elite minions + faster attacks.
/// </summary>
public class DarkLord : BossEnemy
{
    [Header("Dark Lord Config")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private string summonEnemyId = "gargoyle";
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private float _projectileDamage = 2f;
    [SerializeField] private int _fanCount = 5;
    [SerializeField] private float _fanSpread = 60f;
    [SerializeField] private int _ringCount = 12;
    [SerializeField] private int _eliteSummonCount = 3;
    [SerializeField] private float _teleportRange = 8f;

    private int _attackPattern;

    public override void Initialize(EnemyData data)
    {
        _phaseThresholds = new float[] { 0.6f, 0.25f }; // Two phase transitions
        _expMultiplier = 80;
        _goldMultiplier = 30;
        _attackInterval = 2.5f;
        _isUnkillable = false;

        base.Initialize(data);
        _attackPattern = 0;
    }

    protected override void FixedUpdate()
    {
        if (_phaseTransitioning) return;

        // Dark Lord moves slowly, relying on ranged attacks
        var player = GetPlayerController();
        if (player == null) return;

        float speedMultiplier = _currentPhase >= 2 ? 1.5f : 1f;
        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _moveSpeed * 0.6f * speedMultiplier * Time.fixedDeltaTime);
    }

    protected override void ExecuteAttack()
    {
        if (_phaseTransitioning) return;

        switch (_attackPattern % GetAttackCount())
        {
            case 0:
                FanAttack();
                break;
            case 1:
                if (_currentPhase >= 1)
                    RingAttack();
                else
                    FanAttack();
                break;
            case 2:
                if (_currentPhase >= 1)
                    Teleport();
                else
                    FanAttack();
                break;
            case 3:
                if (_currentPhase >= 2)
                    SummonElites();
                else
                    FanAttack();
                break;
        }
        _attackPattern++;
    }

    private int GetAttackCount()
    {
        if (_currentPhase >= 2) return 4;
        if (_currentPhase >= 1) return 3;
        return 1;
    }

    /// <summary>Fire a fan of projectiles aimed at the player.</summary>
    private void FanAttack()
    {
        if (_projectilePrefab == null) return;
        var player = GetPlayerController();
        if (player == null) return;

        Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        float dmgScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.DamageMultiplier : 1f;
        float damage = _projectileDamage * dmgScale;

        int count = _currentPhase >= 2 ? _fanCount + 3 : _fanCount;
        float spread = _currentPhase >= 2 ? _fanSpread + 20f : _fanSpread;

        for (int i = 0; i < count; i++)
        {
            float angleOffset = Mathf.Lerp(-spread / 2f, spread / 2f, count <= 1 ? 0f : (float)i / (count - 1));
            float angle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            var bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.Initialize(damage, _projectileSpeed, dir);
            }
        }
    }

    /// <summary>Fire projectiles in a full ring pattern.</summary>
    private void RingAttack()
    {
        if (_projectilePrefab == null) return;

        float dmgScale = DifficultyManager.HasInstance ? DifficultyManager.Instance.DamageMultiplier : 1f;
        float damage = _projectileDamage * dmgScale;
        int count = _currentPhase >= 2 ? _ringCount + 6 : _ringCount;

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject proj = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            var bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.Initialize(damage, _projectileSpeed * 0.8f, dir);
            }
        }
    }

    /// <summary>Teleport to a random position near the player.</summary>
    private void Teleport()
    {
        var player = GetPlayerController();
        if (player == null) return;

        // Flash effect before teleport
        if (_sr != null) _sr.color = Color.cyan;

        this.InvokeDelayed(0.3f, () =>
        {
            Vector2 playerPos = (Vector2)player.transform.position;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _teleportRange;
            transform.position = playerPos + offset;

            if (_sr != null && !_flashing) _sr.color = _originalColor != default ? _originalColor : Color.white;

            // Fire ring after teleporting
            RingAttack();
        });
    }

    /// <summary>Summon elite enemies near the boss.</summary>
    private void SummonElites()
    {
        var summonData = EnemyDatabase.Instance != null ? EnemyDatabase.Instance.GetById(summonEnemyId) : null;
        if (summonData == null || summonData.prefab == null) return;

        int count = _currentPhase >= 2 ? _eliteSummonCount + 1 : _eliteSummonCount;

        for (int i = 0; i < count; i++)
        {
            if (EnemyBase.ActiveEnemyCount >= 490) break;

            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            var enemyObj = PoolManager.Instance.Get<EnemyBase>(summonData.enemyName);
            if (enemyObj != null)
            {
                enemyObj.transform.position = spawnPos;
                enemyObj.Initialize(summonData);
                enemyObj.SetElite();
            }
        }
    }

    protected override void OnPhaseChanged(int newPhase)
    {
        base.OnPhaseChanged(newPhase);

        // Phase transitions: teleport and fire ring
        Teleport();
    }
}
