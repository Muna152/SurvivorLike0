using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static spatial hash grid for O(1) proximity queries on enemies.
/// Uses a single HashSet for tracking — no dual-collection sync bugs.
/// Reconcile() is called before every query to guarantee correctness.
/// </summary>
public static class SpatialGrid
{
    private const float CellSize = 8f;
    private const float InvCellSize = 1f / CellSize;

    private static readonly Dictionary<long, List<EnemyBase>> _cells =
        new Dictionary<long, List<EnemyBase>>(64);

    // Single source of truth for registered enemies (no dual-collection sync issues)
    private static readonly HashSet<EnemyBase> _registered = new HashSet<EnemyBase>();

    // Reusable result list for the no-arg QueryInRadius overload
    private static readonly List<EnemyBase> _queryResult = new List<EnemyBase>(32);

    // ── Diagnostics ────────────────────────────────────────────────

    public static int CellCount => _cells.Count;
    public static int RegisteredCount => _registered.Count;

    // ── Cell key helpers ──────────────────────────────────────────

    public static long CellKey(int cx, int cy)
    {
        return ((long)cx << 32) | (uint)cy;
    }

    public static void CellCoord(Vector2 pos, out int cx, out int cy)
    {
        cx = (int)(pos.x * InvCellSize + (pos.x >= 0f ? 0.5f : -0.5f));
        cy = (int)(pos.y * InvCellSize + (pos.y >= 0f ? 0.5f : -0.5f));
    }

    public static long ComputeCellKey(Vector2 pos)
    {
        CellCoord(pos, out int cx, out int cy);
        return CellKey(cx, cy);
    }

    // ── Register / Unregister ─────────────────────────────────────

    public static void Register(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (_registered.Contains(enemy))
        {
            // Already registered — force-update cell in case position changed
            ForceUpdateCell(enemy);
            return;
        }
        long key = ComputeCellKey(enemy.transform.position);
        AddToCell(key, enemy);
        enemy.LastCellKey = key;
        _registered.Add(enemy);
    }

    public static void Unregister(EnemyBase enemy)
    {
        if (enemy == null) return;
        if (!_registered.Contains(enemy)) return;

        long key = enemy.LastCellKey;
        RemoveFromCell(key, enemy);
        enemy.LastCellKey = 0;
        _registered.Remove(enemy);
    }

    /// <summary>
    /// Returns true if the enemy is currently registered in the grid.
    /// </summary>
    public static bool IsRegistered(EnemyBase enemy)
    {
        return enemy != null && _registered.Contains(enemy);
    }

    // ── Batch update (called once per FixedUpdate) ────────────────

    /// <summary>
    /// Centralised grid refresh. Call once per physics frame from a single driver.
    /// Updates all registered enemies every frame (no stagger) to guarantee
    /// spatial consistency.
    /// </summary>
    public static void UpdateAll()
    {
        foreach (var e in _registered)
        {
            if (e == null) continue;

            long newKey = ComputeCellKey(e.transform.position);
            if (newKey == e.LastCellKey) continue;

            RemoveFromCell(e.LastCellKey, e);
            AddToCell(newKey, e);
            e.LastCellKey = newKey;
        }
    }

    // ── Reconcile (called before queries) ──────────────────────────

    /// <summary>
    /// Force-updates all registered enemies' cells to match their current positions.
    /// Called before every query to guarantee correctness, even if UpdateAll
    /// hasn't run yet (e.g., on the same frame as registration).
    /// Cost: one hash computation per registered enemy — negligible.
    /// </summary>
    public static void Reconcile()
    {
        foreach (var e in _registered)
        {
            if (e == null) continue;

            long correctKey = ComputeCellKey(e.transform.position);
            if (correctKey == e.LastCellKey) continue;

            RemoveFromCell(e.LastCellKey, e);
            AddToCell(correctKey, e);
            e.LastCellKey = correctKey;
        }
    }

    // ── Queries ───────────────────────────────────────────────────

    /// <summary>
    /// Progressive nearest-enemy search: tries a small radius first,
    /// then expands to the full radius only if needed.
    /// </summary>
    public static EnemyBase QueryNearest(Vector2 center, float maxRadius)
    {
        // Ensure grid cells are up-to-date before querying (same as QueryInRadius)
        Reconcile();

        // Fast path: small radius covers most gameplay cases
        var near = QueryNearestInRadius(center, 15f);
        if (near != null) return near;

        // Fallback: full radius
        return QueryNearestInRadius(center, maxRadius);
    }

    private static EnemyBase QueryNearestInRadius(Vector2 center, float radius)
    {
        float radiusSq = radius * radius;
        int cellRadius = Mathf.CeilToInt(radius * InvCellSize);

        CellCoord(center, out int cx, out int cy);

        EnemyBase nearest = null;
        float nearestDistSq = float.MaxValue;

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                long key = CellKey(cx + dx, cy + dy);
                if (!_cells.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null) continue;
                    float distSq = (center - (Vector2)e.transform.position).sqrMagnitude;
                    if (distSq < nearestDistSq && distSq <= radiusSq)
                    {
                        nearestDistSq = distSq;
                        nearest = e;
                    }
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Find all enemies within `radius` of `center`.
    /// Writes results into the provided list (cleared first).
    /// Reconcile() is called first to guarantee cell correctness.
    /// </summary>
    public static void QueryInRadius(Vector2 center, float radius, List<EnemyBase> results)
    {
        Reconcile();
        results.Clear();
        float radiusSq = radius * radius;
        int cellRadius = Mathf.CeilToInt(radius * InvCellSize);

        CellCoord(center, out int cx, out int cy);

#if UNITY_EDITOR
        RecordQuery(center, radius, cellRadius, cx, cy);
#endif

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                long key = CellKey(cx + dx, cy + dy);
                if (!_cells.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null) continue;
                    if (((center - (Vector2)e.transform.position).sqrMagnitude) <= radiusSq)
                        results.Add(e);
                }
            }
        }
    }

    /// <summary>
    /// Convenience overload: uses an internal reusable list.
    /// Returned list is invalidated on the next call to this overload.
    /// </summary>
    public static List<EnemyBase> QueryInRadius(Vector2 center, float radius)
    {
        Reconcile();
        _queryResult.Clear();
        float radiusSq = radius * radius;
        int cellRadius = Mathf.CeilToInt(radius * InvCellSize);

        CellCoord(center, out int cx, out int cy);

#if UNITY_EDITOR
        RecordQuery(center, radius, cellRadius, cx, cy);
#endif

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                long key = CellKey(cx + dx, cy + dy);
                if (!_cells.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null) continue;
                    if (((center - (Vector2)e.transform.position).sqrMagnitude) <= radiusSq)
                        _queryResult.Add(e);
                }
            }
        }

        return _queryResult;
    }

    // ── Debug visualisation ────────────────────────────────────────

#if UNITY_EDITOR
    public struct QueryDebugInfo
    {
        public Vector2 Center;
        public float Radius;
        public int CellRadius;
        public int Cx, Cy;
    }

    public static QueryDebugInfo LastQuery;
    public static bool HasLastQuery;

    private static void RecordQuery(Vector2 center, float radius, int cellRadius, int cx, int cy)
    {
        LastQuery = new QueryDebugInfo
        {
            Center = center,
            Radius = radius,
            CellRadius = cellRadius,
            Cx = cx,
            Cy = cy
        };
        HasLastQuery = true;
    }

    public static void DrawDebugGizmos()
    {
        if (!HasLastQuery) return;

        var q = LastQuery;

        // --- Red circle: damage radius ---
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            var p1 = new Vector3(q.Center.x + Mathf.Cos(a1) * q.Radius,
                                 q.Center.y + Mathf.Sin(a1) * q.Radius, 0f);
            var p2 = new Vector3(q.Center.x + Mathf.Cos(a2) * q.Radius,
                                 q.Center.y + Mathf.Sin(a2) * q.Radius, 0f);
            Gizmos.DrawLine(p1, p2);
        }

        // --- Green wire cubes: scanned grid cells ---
        for (int dx = -q.CellRadius; dx <= q.CellRadius; dx++)
        {
            for (int dy = -q.CellRadius; dy <= q.CellRadius; dy++)
            {
                int cellX = q.Cx + dx;
                int cellY = q.Cy + dy;
                var cellCenter = new Vector3(cellX * CellSize, cellY * CellSize, 0f);
                var cellSize = new Vector3(CellSize, CellSize, 0f);

                Gizmos.color = new Color(0f, 1f, 0f, 0.06f);
                Gizmos.DrawCube(cellCenter, cellSize);
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                Gizmos.DrawWireCube(cellCenter, cellSize);
            }
        }

        // --- Enemy dots ---
        float radiusSq = q.Radius * q.Radius;
        for (int dx = -q.CellRadius; dx <= q.CellRadius; dx++)
        {
            for (int dy = -q.CellRadius; dy <= q.CellRadius; dy++)
            {
                long key = CellKey(q.Cx + dx, q.Cy + dy);
                if (!_cells.TryGetValue(key, out var list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null) continue;

                    float distSq = (q.Center - (Vector2)e.transform.position).sqrMagnitude;
                    Gizmos.color = distSq <= radiusSq
                        ? new Color(1f, 0.2f, 0.2f, 0.9f)
                        : new Color(1f, 1f, 0f, 0.6f);
                    Gizmos.DrawSphere(e.transform.position, 0.25f);
                }
            }
        }
    }
#endif

    // ── Lifecycle ──────────────────────────────────────────────────

    public static void Clear()
    {
        foreach (var kvp in _cells)
            kvp.Value.Clear();
        _cells.Clear();
        _registered.Clear();
        _queryResult.Clear();
    }

    // ── Internals ──────────────────────────────────────────────────

    private static void ForceUpdateCell(EnemyBase enemy)
    {
        long correctKey = ComputeCellKey(enemy.transform.position);
        if (correctKey == enemy.LastCellKey) return;

        RemoveFromCell(enemy.LastCellKey, enemy);
        AddToCell(correctKey, enemy);
        enemy.LastCellKey = correctKey;
    }

    private static void AddToCell(long key, EnemyBase enemy)
    {
        if (!_cells.TryGetValue(key, out var list))
        {
            list = new List<EnemyBase>(8);
            _cells[key] = list;
        }
        list.Add(enemy);
    }

    private static void RemoveFromCell(long key, EnemyBase enemy)
    {
        if (!_cells.TryGetValue(key, out var list)) return;
        list.Remove(enemy);
        if (list.Count == 0)
            _cells.Remove(key);
    }
}
