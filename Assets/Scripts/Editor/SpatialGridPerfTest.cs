using System.Diagnostics;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor-only performance benchmark for SpatialGrid.
/// Menu → Tools → Benchmark SpatialGrid
/// </summary>
public static class SpatialGridPerfTest
{
    private const int EnemyCount = 500;
    private const int Iterations = 1000;

    [MenuItem("Tools/Benchmark SpatialGrid")]
    public static void Run()
    {
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log("  SpatialGrid Performance Benchmark");
        UnityEngine.Debug.Log($"  Enemies: {EnemyCount} | Iterations: {Iterations}");
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Create dummy enemy GameObjects spread in 60x60 area
        var enemies = new EnemyBase[EnemyCount];
        var goParent = new GameObject("_PerfTest_Enemies");

        for (int i = 0; i < EnemyCount; i++)
        {
            var go = new GameObject($"E{i}");
            go.transform.parent = goParent.transform;
            float x = Random.Range(-30f, 30f);
            float y = Random.Range(-30f, 30f);
            go.transform.position = new Vector3(x, y, 0);

            var enemy = go.AddComponent<EnemyBase>();
            EnemyBase.ActiveEnemies.Add(enemy);
            SpatialGrid.Register(enemy);
            enemies[i] = enemy;
        }

        Vector2 center = Vector2.zero;
        var sw = new Stopwatch();

        // ── 1. Old O(n) linear scan ────────────────────────────────
        sw.Restart();
        for (int iter = 0; iter < Iterations; iter++)
        {
            float best = float.MaxValue;
            foreach (var e in EnemyBase.ActiveEnemies)
            {
                if (e == null) continue;
                float d = Vector2.Distance(center, (Vector2)e.transform.position);
                if (d < best) best = d;
            }
        }
        sw.Stop();
        long linearUs = Us(sw);
        Log("LinearScan (all)", linearUs);

        // ── 2. QueryNearest with different radii ───────────────────
        foreach (float r in new[] { 10f, 15f, 25f, 50f })
        {
            sw.Restart();
            for (int iter = 0; iter < Iterations; iter++)
                SpatialGrid.QueryNearest(center, r);
            sw.Stop();
            long t = Us(sw);
            Log($"QueryNearest(r={r:F0})", t, linearUs);
        }

        // ── 3. QueryInRadius with different radii ──────────────────
        foreach (float r in new[] { 3f, 5f, 10f })
        {
            int hits = 0;
            sw.Restart();
            for (int iter = 0; iter < Iterations; iter++)
            {
                var list = SpatialGrid.QueryInRadius(center, r);
                hits += list.Count;
            }
            sw.Stop();
            long t = Us(sw);
            Log($"QueryInRadius(r={r:F0})", t, linearUs, $"avg {hits / (float)Iterations:F1} hits");
        }

        // ── 4. UpdateAll (all enemies per frame) ──────────────────
        const int frames = 300;
        sw.Restart();
        for (int f = 0; f < frames; f++)
            SpatialGrid.UpdateAll();
        sw.Stop();
        long updateUs = Us(sw);
        Log($"UpdateAll (no stagger)", updateUs, perFrame: (float)updateUs / frames);

        // ── 5. Correctness check ───────────────────────────────────
        var oldNearest = LinearNearest(center);
        var gridNearest = SpatialGrid.QueryNearest(center, 50f);
        bool pass = oldNearest == gridNearest;

        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        UnityEngine.Debug.Log($"  Correctness: {(pass ? "✅ PASS" : "❌ FAIL")}");
        UnityEngine.Debug.Log($"  Cells: {SpatialGrid.CellCount} | Registered: {SpatialGrid.RegisteredCount}");
        UnityEngine.Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // Cleanup
        foreach (var e in enemies)
        {
            if (e == null) continue;
            EnemyBase.ActiveEnemies.Remove(e);
            SpatialGrid.Unregister(e);
        }
        Object.DestroyImmediate(goParent);
        EnemyBase.ResetStatics();
        SpatialGrid.Clear();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static long Us(Stopwatch sw) =>
        sw.ElapsedTicks * 1000000L / Stopwatch.Frequency;

    private static EnemyBase LinearNearest(Vector2 center)
    {
        float best = float.MaxValue;
        EnemyBase nearest = null;
        foreach (var e in EnemyBase.ActiveEnemies)
        {
            if (e == null) continue;
            float d = Vector2.Distance(center, (Vector2)e.transform.position);
            if (d < best) { best = d; nearest = e; }
        }
        return nearest;
    }

    private static void Log(string label, long totalUs, long? baselineUs = null, string extra = null, float? perFrame = null)
    {
        string speedup = baselineUs.HasValue ? $" | {(float)baselineUs.Value / totalUs:F1}x vs linear" : "";
        string perCall = perFrame.HasValue ? $" | {perFrame.Value:F1} µs/frame" : $" | {(float)totalUs / Iterations:F1} µs/call";
        string suffix = extra != null ? $" | {extra}" : "";
        UnityEngine.Debug.Log($"  [{label}] {totalUs} µs total{perCall}{speedup}{suffix}");
    }
}
