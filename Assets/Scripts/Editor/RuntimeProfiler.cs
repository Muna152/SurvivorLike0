using UnityEngine;

/// <summary>
/// Temporary runtime profiler — adds itself, samples N frames, reports, then self-destructs.
/// Usage: execute_csharp_script → new GameObject().AddComponent&lt;RuntimeProfiler&gt;();
/// </summary>
public class RuntimeProfiler : MonoBehaviour
{
    private int _remainingFrames;
    private float _totalDT;
    private float _minDT = float.MaxValue;
    private float _maxDT;
    private long _gcMemBefore;
    private int _gc0Before, _gc1Before, _gc2Before;
    private long _profilerAllocBefore;

    private void Start()
    {
        _remainingFrames = 120;
        _gcMemBefore = System.GC.GetTotalMemory(false);
        _gc0Before = System.GC.CollectionCount(0);
        _gc1Before = System.GC.CollectionCount(1);
        _gc2Before = System.GC.CollectionCount(2);
        _profilerAllocBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        Debug.Log("[RuntimeProfiler] Sampling 120 frames...");
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _totalDT += dt;
        if (dt < _minDT) _minDT = dt;
        if (dt > _maxDT) _maxDT = dt;

        _remainingFrames--;
        if (_remainingFrames <= 0)
        {
            ReportAndDestroy();
        }
    }

    private void ReportAndDestroy()
    {
        int frames = 120;
        float avgDT = _totalDT / frames;
        float avgFPS = 1f / avgDT;
        float minFPS = 1f / _maxDT;
        float maxFPS = 1f / _minDT;

        long gcMemAfter = System.GC.GetTotalMemory(false);
        int gc0After = System.GC.CollectionCount(0);
        int gc1After = System.GC.CollectionCount(1);
        int gc2After = System.GC.CollectionCount(2);
        long profilerAllocAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();

        Debug.Log("╔══════════════════════════════════════════╗");
        Debug.Log("║        RUNTIME PROFILER REPORT          ║");
        Debug.Log("╚══════════════════════════════════════════╝");

        // --- Frame Timing ---
        Debug.Log($"[Frame Timing] {frames} frames sampled");
        Debug.Log($"  Avg: {avgDT * 1000f:F2}ms ({avgFPS:F1} FPS)");
        Debug.Log($"  Best: {_minDT * 1000f:F2}ms ({maxFPS:F1} FPS)");
        Debug.Log($"  Worst: {_maxDT * 1000f:F2}ms ({minFPS:F1} FPS)");
        Debug.Log($"  Jitter (max-avg): {(_maxDT - avgDT) * 1000f:F2}ms");

        // --- GC ---
        long gcHeapDeltaMB = (gcMemAfter - _gcMemBefore) / (1024 * 1024);
        Debug.Log($"[GC & Memory]");
        Debug.Log($"  GC Heap: {gcMemAfter / (1024*1024)}MB (delta: {gcHeapDeltaMB}MB)");
        Debug.Log($"  Profiler Alloc: {profilerAllocAfter / (1024*1024)}MB (delta: {(profilerAllocAfter - _profilerAllocBefore) / 1024}KB)");
        Debug.Log($"  Gen0 collections: {gc0After - _gc0Before}");
        Debug.Log($"  Gen1 collections: {gc1After - _gc1Before}");
        Debug.Log($"  Gen2 collections: {gc2After - _gc2Before}");
        Debug.Log($"  TotalAllocated: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024*1024)}MB");
        Debug.Log($"  TotalReserved: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024*1024)}MB");

        // --- Scene Objects ---
        var enemyBaseType = System.Type.GetType("EnemyBase, Assembly-CSharp");
        int activeEnemyCount = 0;
        if (enemyBaseType != null)
        {
            var prop = enemyBaseType.GetProperty("ActiveEnemyCount",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop != null) activeEnemyCount = (int)prop.GetValue(null);
        }

        var allGOs = FindObjectsOfType<GameObject>();
        int enemies = 0, projectiles = 0, drops = 0, vfx = 0;
        foreach (var go in allGOs)
        {
            var n = go.name.ToLower();
            if (n.Contains("enemy") || n.Contains("skeleton") || n.Contains("dark") || n.Contains("death") || n.Contains("boss")) enemies++;
            else if (n.Contains("projectile") || n.Contains("bullet") || n.Contains("knife") || n.Contains("orbital")) projectiles++;
            else if (n.Contains("drop") || n.Contains("exp") || n.Contains("gem") || n.Contains("gold") || n.Contains("chest")) drops++;
            else if (n.Contains("vfx") || n.Contains("damage") || n.Contains("effect")) vfx++;
        }

        Debug.Log($"[Scene Objects]");
        Debug.Log($"  GameObjects: {allGOs.Length} | ActiveEnemies: {activeEnemyCount}");
        Debug.Log($"  Enemies: {enemies} | Projectiles: {projectiles} | Drops: {drops} | VFX: {vfx}");
        Debug.Log($"  Rigidbodies2D: {FindObjectsOfType<Rigidbody2D>().Length}");
        Debug.Log($"  Colliders2D: {FindObjectsOfType<Collider2D>().Length}");
        Debug.Log($"  SpriteRenderers: {FindObjectsOfType<SpriteRenderer>().Length}");
        Debug.Log($"  AudioSources: {FindObjectsOfType<AudioSource>().Length}");

        // --- Rendering ---
        Debug.Log($"[Rendering]");
        Debug.Log($"  DrawCalls: {UnityEditor.UnityStats.drawCalls} | Batches: {UnityEditor.UnityStats.batches}");
        Debug.Log($"  Triangles: {UnityEditor.UnityStats.triangles} | Vertices: {UnityEditor.UnityStats.vertices}");

        // --- Time ---
        Debug.Log($"[Time] GameTime: {Time.time:F1}s | TimeScale: {Time.timeScale} | FixedDT: {Time.fixedDeltaTime:F4}s");

        Debug.Log("╔══════════════════════════════════════════╗");
        Debug.Log("║        PROFILER REPORT COMPLETE         ║");
        Debug.Log("╚══════════════════════════════════════════╝");

        Destroy(gameObject);
    }
}
