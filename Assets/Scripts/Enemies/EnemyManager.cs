using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton managing all active enemies in the scene.
/// Drives centralized update via IEnemyTick. Uses EnemyBase.ActiveEnemies
/// (static HashSet) as the single source of truth — all enemies register
/// there in Initialize(), regardless of spawn path.
/// </summary>
public class EnemyManager : Singleton<EnemyManager>
{
    private PlayerStats _playerStats;

    // Symmetric separation: accumulates push vectors per enemy, applied once at the end
    private readonly Dictionary<int, Vector2> _separationPushes = new Dictionary<int, Vector2>(64);

    // Separation constants
    private const float OverlapAllowance = 0.23f; // Allow ~77% overlap (gap ≈ 1/3 of original)
    private const float PushFactor = 0.5f;       // Smoothing per frame
    private const int SeparationInterval = 2;    // Run separation every N FixedFrames

    // Forward-only adjacent cell directions for inter-cell pair processing (avoids redundant pairs)
    private static readonly int[][] AdjacentDirs =
    {
        new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 }, new[] { -1, 1 }
    };

    public int ActiveCount => EnemyBase.ActiveEnemyCount;
    /// <summary>True when spawner should pause due to enemy cap.</summary>
    public bool IsSpawnCapped => EnemyBase.ActiveEnemyCount >= (GameBalanceConfig.Instance != null ? GameBalanceConfig.Instance.maxEnemiesOnScreen : 200);

    protected override void Awake()
    {
        base.Awake();
        GameEvents.OnEnemyDied += OnEnemyDiedHandler;
    }

    private void Start()
    {
        _playerStats = FindObjectOfType<PlayerStats>();
    }

    public EnemyBase SpawnEnemy(EnemyData data, Vector2 position)
    {
        var enemyObj = PoolManager.Instance.Get<EnemyBase>(data.enemyName);
        if (enemyObj == null) return null;

        enemyObj.transform.position = position;
        enemyObj.Initialize(data);
        GameEvents.InvokeEnemySpawned(enemyObj);
        return enemyObj;
    }

    private void OnEnemyDiedHandler(EnemyBase enemy)
    {
        // Kill tracking is handled in EnemyBase.Die() — no duplicate counting here
    }

    private void FixedUpdate()
    {
        SpatialGrid.UpdateAll();

        float dt = Time.fixedDeltaTime;
        EnemyBase.IsIterating = true;
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null)
                enemy.OnFixedTick(dt);
        }
        EnemyBase.IsIterating = false;
        EnemyBase.FlushPendingRemoves();

        // Separation pass: run every other frame to halve CPU cost.
        // Skip far-LOD enemies — they don't need pixel-perfect separation.
        if (Time.frameCount % SeparationInterval == 0)
            SeparationPass();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        EnemyBase.IsIterating = true;
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null)
                enemy.OnUpdateTick(dt);
        }
        EnemyBase.IsIterating = false;
        EnemyBase.FlushPendingRemoves();
    }

    /// <summary>
    /// Soft separation pass: push enemies apart when they overlap more than the allowance.
    /// Uses cell-based iteration with symmetric pair processing: each pair is computed
    /// exactly once, and half-push is applied to both enemies. This halves the number
    /// of distance checks compared to the previous per-enemy radius query approach.
    /// Skips far-LOD enemies to save CPU when enemy count is high.
    /// </summary>
    private void SeparationPass()
    {
        _separationPushes.Clear();

        // Iterate over all cells, process pairs within each cell and with adjacent cells
        SpatialGrid.IterateCells((cellKey, enemies) =>
        {
            // --- Intra-cell pairs: check all pairs within this cell ---
            for (int i = 0; i < enemies.Count; i++)
            {
                var a = enemies[i];
                if (a == null || a.IsFarLOD) continue;
                float r1 = a.SeparationRadius;
                if (r1 <= 0f) continue;

                for (int j = i + 1; j < enemies.Count; j++)
                {
                    var b = enemies[j];
                    if (b == null || b.IsFarLOD) continue;
                    float r2 = b.SeparationRadius;
                    if (r2 <= 0f) continue;

                    ProcessSeparationPair(a, b, r1, r2);
                }
            }

            // --- Inter-cell pairs: check against 4 forward neighbors (right, below, below-right, below-left) ---
            // Only process "forward" directions to avoid processing the same pair twice
            // (A→B in cell(0,0) vs B→A in cell(1,0) is the same pair)
            SpatialGrid.CellCoordFromKey(cellKey, out int cx, out int cy);

            for (int d = 0; d < AdjacentDirs.Length; d++)
            {
                long adjKey = SpatialGrid.CellKey(cx + AdjacentDirs[d][0], cy + AdjacentDirs[d][1]);
                if (!SpatialGrid.TryGetCell(adjKey, out var adjEnemies)) continue;

                for (int i = 0; i < enemies.Count; i++)
                {
                    var a = enemies[i];
                    if (a == null || a.IsFarLOD) continue;
                    float r1 = a.SeparationRadius;
                    if (r1 <= 0f) continue;

                    for (int j = 0; j < adjEnemies.Count; j++)
                    {
                        var b = adjEnemies[j];
                        if (b == null || b.IsFarLOD) continue;
                        float r2 = b.SeparationRadius;
                        if (r2 <= 0f) continue;

                        ProcessSeparationPair(a, b, r1, r2);
                    }
                }
            }
        });

        ApplyAccumulatedPushes();
    }

    /// <summary>
    /// Process a single enemy pair for separation: compute overlap push and apply
    /// symmetrically (half to each). Accumulates push vectors per enemy.
    /// </summary>
    private void ProcessSeparationPair(EnemyBase a, EnemyBase b, float r1, float r2)
    {
        float minDist = (r1 + r2) * OverlapAllowance;
        Vector2 diff = (Vector2)a.transform.position - (Vector2)b.transform.position;
        float distSq = diff.sqrMagnitude;

        if (distSq >= minDist * minDist) return;

        float dist = Mathf.Sqrt(distSq);
        Vector2 pushDir;
        float overlap;

        if (dist > 0.001f)
        {
            pushDir = diff / dist;
            overlap = minDist - dist;
        }
        else
        {
            // Coincident enemies — deterministic push direction
            float angle = ((a.GetInstanceID() * 2.345f) % 6.283185f);
            pushDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            overlap = minDist;
        }

        // Symmetric push: half to each enemy
        Vector2 pushA = pushDir * (overlap * 0.5f);
        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();

        if (!_separationPushes.TryGetValue(idA, out var pushForA))
            pushForA = Vector2.zero;
        if (!_separationPushes.TryGetValue(idB, out var pushForB))
            pushForB = Vector2.zero;

        _separationPushes[idA] = pushForA + pushA;
        _separationPushes[idB] = pushForB - pushA; // Opposite direction
    }

    /// <summary>
    /// Applies the accumulated separation push vectors to each enemy.
    /// Uses a parallel list of enemy references to avoid dictionary lookups per frame.
    /// </summary>
    private readonly List<EnemyBase> _pushTargets = new List<EnemyBase>(64);

    private void ApplyAccumulatedPushes()
    {
        // Collect all active enemies once for ID→reference resolution
        _pushTargets.Clear();
        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e != null && !e.IsFarLOD && _separationPushes.ContainsKey(e.GetInstanceID()))
                _pushTargets.Add(e);
        }

        for (int i = 0; i < _pushTargets.Count; i++)
        {
            var enemy = _pushTargets[i];
            var push = _separationPushes[enemy.GetInstanceID()];
            if (push.sqrMagnitude > 0.0001f)
                enemy.ApplySeparationPush(push * PushFactor);
        }
    }

    protected override void OnDestroy()
    {
        GameEvents.OnEnemyDied -= OnEnemyDiedHandler;
        base.OnDestroy();
    }
}
