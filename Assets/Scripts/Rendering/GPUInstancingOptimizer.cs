using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GPUInstancingOptimizer : MonoBehaviour
{
    [MenuItem("Tools/Optimization/Enable GPU Instancing on All Materials")]
    public static void EnableGPUInstancingOnAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int enabledCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != null)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                enabledCount++;
                Debug.Log($"[GPUInstancingOptimizer] Enabled instancing on: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GPUInstancingOptimizer] ✓ Enabled instancing on {enabledCount} materials.");
    }

    [MenuItem("Tools/Optimization/Check Material Instancing Status")]
    public static void CheckMaterialInstancingStatus()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int instancingEnabled = 0;
        int totalMaterials = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null)
            {
                totalMaterials++;
                if (mat.enableInstancing)
                {
                    instancingEnabled++;
                    Debug.Log($"[OK] {path} - Instancing: ON, Shader: {mat.shader.name}");
                }
                else
                {
                    Debug.LogWarning($"[MISSING] {path} - Instancing: OFF, Shader: {mat.shader.name}");
                }
            }
        }

        Debug.Log($"[GPUInstancingOptimizer] Instancing enabled on {instancingEnabled}/{totalMaterials} materials.");
    }

    [MenuItem("Tools/Optimization/List All Materials")]
    public static void ListAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        Debug.Log($"[GPUInstancingOptimizer] Found {guids.Length} materials:");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != null)
            {
                Debug.Log($"  - {path}");
                Debug.Log($"    Shader: {mat.shader.name}");
                Debug.Log($"    Instancing: {(mat.enableInstancing ? "ON" : "OFF")}");
            }
        }
    }
}
