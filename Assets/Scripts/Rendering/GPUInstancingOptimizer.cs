using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// GPU Instancing优化器 - 为场景中实际使用的材质启用GPU Instancing
/// 
/// 修复版本:
/// 1. 修复优化目标错误问题 - 现在优化场景中实际使用的材质，而不是项目中的材质文件
/// 2. 添加Shader支持检查 - 跳过不支持Instancing的Shader
/// 3. 移除不必要的[ExecuteInEditMode]和MonoBehaviour继承
/// 4. 添加"Enable GPU Instancing on Used Materials"菜单项
/// </summary>
public static class GPUInstancingOptimizer
{
    /// <summary>
    /// 为场景中实际使用的材质启用GPU Instancing
    /// 这是主要的修复方法，确保优化正确的材质
    /// </summary>
    [MenuItem("Tools/Optimization/Enable GPU Instancing on Used Materials")]
    public static void EnableGPUInstancingOnUsedMaterials()
    {
        Debug.Log("[GPUInstancingOptimizer] Starting material optimization for actually used materials...");
        
        // 获取场景中实际使用的材质
        HashSet<Material> usedMaterials = new HashSet<Material>();
        SpriteRenderer[] renderers = GameObject.FindObjectsOfType<SpriteRenderer>(true);
        
        Debug.Log($"[GPUInstancingOptimizer] Found {renderers.Length} SpriteRenderers in scene");
        
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial != null)
            {
                usedMaterials.Add(renderer.sharedMaterial);
            }
        }
        
        Debug.Log($"[GPUInstancingOptimizer] Found {usedMaterials.Count} unique materials being used");
        
        // 只优化实际使用的材质
        int enabledCount = 0;
        int skippedCount = 0;
        
        foreach (Material mat in usedMaterials)
        {
            if (mat == null) continue;
            
            string path = AssetDatabase.GetAssetPath(mat);
            
            // 检查shader是否支持instancing
            bool supportInstancing = IsShaderSupportsInstancing(mat.shader);
            
            if (!supportInstancing)
            {
                Debug.LogWarning($"[GPUInstancingOptimizer] Skipped {mat.name} - Shader not supported: {mat.shader?.name}");
                skippedCount++;
                continue;
            }
            
            if (!mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                enabledCount++;
                
                string pathInfo = string.IsNullOrEmpty(path) ? "[Built-in]" : path;
                Debug.Log($"[GPUInstancingOptimizer] Enabled instancing on: {mat.name} ({pathInfo})");
            }
            else
            {
                Debug.Log($"[GPUInstancingOptimizer] {mat.name} already has instancing enabled");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[GPUInstancingOptimizer] Optimization complete!");
        Debug.Log($"[GPUInstancingOptimizer] Enabled instancing on {enabledCount} materials");
        Debug.Log($"[GPUInstancingOptimizer] Skipped {skippedCount} materials (not supported or already enabled)");
    }
    
    /// <summary>
    /// 为项目中的所有材质启用GPU Instancing（原始方法，已标记为辅助方法）
    /// </summary>
    [MenuItem("Tools/Optimization/Enable GPU Instancing on All Materials")]
    public static void EnableGPUInstancingOnAllMaterials()
    {
        Debug.Log("[GPUInstancingOptimizer] Scanning for all material files in project...");
        
        string[] guids = AssetDatabase.FindAssets("t:Material");
        Debug.Log($"[GPUInstancingOptimizer] Found {guids.Length} material files");
        
        int enabledCount = 0;
        int skippedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null && mat.shader != null)
            {
                bool supportInstancing = IsShaderSupportsInstancing(mat.shader);
                
                if (!supportInstancing)
                {
                    Debug.LogWarning($"[GPUInstancingOptimizer] Skipped {path} - Shader not supported: {mat.shader.name}");
                    skippedCount++;
                    continue;
                }
                
                if (!mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    enabledCount++;
                    Debug.Log($"[GPUInstancingOptimizer] Enabled instancing on: {path}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[GPUInstancingOptimizer] Enabled instancing on {enabledCount} materials");
        Debug.Log($"[GPUInstancingOptimizer] Skipped {skippedCount} materials");
    }
    
    /// <summary>
    /// 分析场景中的材质使用情况
    /// </summary>
    [MenuItem("Tools/Optimization/Analyze Material Usage")]
    public static void AnalyzeMaterialUsage()
    {
        Debug.Log("=== Material Usage Analysis ===");
        
        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        Dictionary<string, int> materialUsageCount = new Dictionary<string, int>();
        
        SpriteRenderer[] renderers = GameObject.FindObjectsOfType<SpriteRenderer>(true);
        Debug.Log($"Found {renderers.Length} SpriteRenderers");
        
        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial != null)
            {
                uniqueMaterials.Add(renderer.sharedMaterial);
                
                string matName = renderer.sharedMaterial.name;
                string path = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                string key = string.IsNullOrEmpty(path) ? $"[Built-in] {matName}" : path;
                
                if (!materialUsageCount.ContainsKey(key))
                {
                    materialUsageCount[key] = 0;
                }
                materialUsageCount[key]++;
            }
        }
        
        Debug.Log($"Found {uniqueMaterials.Count} unique materials:");
        
        foreach (var kvp in materialUsageCount.OrderByDescending(x => x.Value))
        {
            Debug.Log($"  Material: {kvp.Key} - Used by {kvp.Value} SpriteRenderers");
        }
        
        // 检查每个材质的Instancing状态
        Debug.Log("\n=== Instancing Status ===");
        foreach (var mat in uniqueMaterials)
        {
            string path = AssetDatabase.GetAssetPath(mat);
            string pathInfo = string.IsNullOrEmpty(path) ? "[Built-in]" : path;
            Debug.Log($"  {mat.name} ({pathInfo}): Instancing = {mat.enableInstancing}, Shader = {mat.shader?.name}");
        }
    }
    
    /// <summary>
    /// 检查Shader是否支持GPU Instancing
    /// </summary>
    private static bool IsShaderSupportsInstancing(Shader shader)
    {
        if (shader == null) return false;
        
        string shaderName = shader.name;
        
        // 不支持Instancing的Shader列表
        if (shaderName == "GUI/Text Shader" || 
            shaderName == "GUI/3D Text Shader" ||
            shaderName.StartsWith("Unlit/Transparent"))
        {
            return false;
        }
        
        // 通常支持Instancing的Shader
        if (shaderName.StartsWith("Standard") ||
            shaderName.StartsWith("Universal Render Pipeline") ||
            shaderName.StartsWith("High Definition Render Pipeline") ||
            shaderName.Contains("Lit") ||
            shaderName.Contains("Default"))
        {
            return true;
        }
        
        // 默认情况下，大多数现代Shader都支持Instancing
        return true;
    }
}
