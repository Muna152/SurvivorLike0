using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu items for AutoPlayTester. Separated from the main class
/// to keep AutoPlayTester in the runtime assembly (Assembly-CSharp)
/// while menu items live in the editor assembly.
/// </summary>
public static class AutoPlayTesterMenu
{
    [MenuItem("Tools/AutoPlay Test (1 run)", false, 100)]
    private static void StartAutoPlay1Run()
    {
        var go = new GameObject("AutoPlayTester");
        var tester = go.AddComponent<AutoPlayTester>();
        tester.maxRuns = 1;
        tester.characterIndex = 0;
        tester.sampleInterval = 5f;
        tester.autoUpgrade = true;
        tester.enableProfiling = true;
        Debug.Log("[AutoPlay] Started 1-run test from menu.");
    }

    [MenuItem("Tools/AutoPlay Stress Test (3 runs)", false, 101)]
    private static void StartAutoPlayStress()
    {
        var go = new GameObject("AutoPlayTester");
        var tester = go.AddComponent<AutoPlayTester>();
        tester.maxRuns = 3;
        tester.characterIndex = 0;
        tester.sampleInterval = 5f;
        tester.autoUpgrade = true;
        tester.enableProfiling = true;
        Debug.Log("[AutoPlay] Started 3-run stress test from menu.");
    }

    [MenuItem("Tools/AutoPlay Extended Test (10 runs)", false, 102)]
    private static void StartAutoPlayExtended()
    {
        var go = new GameObject("AutoPlayTester");
        var tester = go.AddComponent<AutoPlayTester>();
        tester.maxRuns = 10;
        tester.characterIndex = 0;
        tester.sampleInterval = 10f;
        tester.autoUpgrade = true;
        tester.enableProfiling = true;
        Debug.Log("[AutoPlay] Started 10-run extended test from menu.");
    }
}
