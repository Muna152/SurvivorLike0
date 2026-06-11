# 游戏性能优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标:** 系统化优化 Roguelike 幸存者游戏性能，提升帧率稳定性和整体游戏体验

**架构:** 分三阶段优化：立即实施(高收益低成本) → 中期优化(中等收益中等成本) → 长期架构升级(高收益高成本)

**技术栈:** Unity 2021.3, Built-in RP → URP, C#, 对象池, 空间分区, 性能分析工具

---

## 阶段一：立即实施优化 (高收益/低成本)

### Task 1: 移除生产环境 Debug.Log

**目标:** 移除生产环境中的所有 Debug.Log 调用，减少字符串分配和I/O开销

**文件:**
- Modify: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Scripts/Player/PlayerController.cs`
- Modify: `Assets/Scripts/Enemies/EnemyManager.cs`
- Modify: `Assets/Scripts/Core/PoolManager.cs`
- Create: `Assets/Scripts/Core/DebugLogger.cs`

- [ ] **Step 1: 创建条件编译日志系统**

```csharp
// Assets/Scripts/Core/DebugLogger.cs
using UnityEngine;

public static class DebugLogger
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
```

- [ ] **Step 2: 替换 GameManager.cs 中的 Debug.Log**

```csharp
// 替换前
Debug.Log($"[GameManager] Game started with character: {(SelectedCharacter != null ? SelectedCharacter.characterName : "none")}");

// 替换后
DebugLogger.Log($"[GameManager] Game started with character: {(SelectedCharacter != null ? SelectedCharacter.characterName : "none")}");
```

- [ ] **Step 3: 替换 PlayerController.cs 中的 Debug.Log**

```csharp
// 替换前
Debug.Log($"[AutoPilot] {(IsAutoPilot ? "ENABLED" : "DISABLED")}");

// 替换后
DebugLogger.Log($"[AutoPilot] {(IsAutoPilot ? "ENABLED" : "DISABLED")}");
```

- [ ] **Step 4: 替换 EnemyManager.cs 中的 Debug.Log**

```csharp
// 替换前
Debug.LogWarning("[GameManager] PopPause called with no matching PushPause.");

// 替换后
DebugLogger.LogWarning("[GameManager] PopPause called with no matching PushPause.");
```

- [ ] **Step 5: 替换 PoolManager.cs 中的 Debug.Log**

```csharp
// 替换前
Debug.LogWarning($"[PoolManager] Pool '{key}' already registered. Skipping.");

// 替换后
DebugLogger.LogWarning($"[PoolManager] Pool '{key}' already registered. Skipping.");
```

- [ ] **Step 6: 运行游戏测试功能正常**

Run: 在 Unity Editor 中运行游戏，验证所有功能正常工作
Expected: 游戏功能正常，编辑器中仍能看到日志

- [ ] **Step 7: 构建测试版本验证日志移除**

Run: `File > Build Settings > Build` 构建测试版本
Expected: 构建成功，运行构建版本无日志输出

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Core/DebugLogger.cs
git add Assets/Scripts/Core/GameManager.cs
git add Assets/Scripts/Player/PlayerController.cs
git add Assets/Scripts/Enemies/EnemyManager.cs
git add Assets/Scripts/Core/PoolManager.cs
git commit -m "perf: replace Debug.Log with conditional compilation logger"
```

---

### Task 2: 优化 Canvas 设置和UI批处理

**目标:** 优化UI Canvas设置，分离静态和动态UI，减少UI重绘开销

**文件:**
- Modify: `Assets/GameLevel.scene` (通过Unity编辑器操作)
- Modify: `Assets/Scripts/UI/HUDController.cs`

- [ ] **Step 1: 分析当前Canvas结构**

Run: 在Unity Editor中选择 `HUD Canvas`，检查Canvas组件设置
Expected: 记录当前Canvas Scaler模式和子对象数量

- [ ] **Step 2: 创建静态UI Canvas**

在Unity Editor中操作：
1. 右键 Hierarchy → UI → Canvas，命名为 `StaticUI Canvas`
2. 设置 Canvas Scaler → UI Scale Mode: `Constant Pixel Size`
3. 设置 Canvas → Render Mode: `Screen Space - Overlay`
4. 移动静态UI元素(如背景、装饰性元素)到新Canvas

- [ ] **Step 3: 优化主HUD Canvas设置**

在Unity Editor中修改 `HUD Canvas`：
1. Canvas Scaler → UI Scale Mode: `Scale With Screen Size`
2. Reference Resolution: `1920x1080`
3. Screen Match Mode: `Match Width Or Height`
4. Match: `0.5` (平衡宽高比)
5. 移除不必要的Graphic Raycaster目标

- [ ] **Step 4: 优化HUDController.cs中的UI更新**

```csharp
// Assets/Scripts/UI/HUDController.cs
// 添加缓存避免频繁GetChild调用
private Transform _hpBarTransform;
private Transform _expBarTransform;
private Text _levelText;

private void Awake()
{
    // 缓存UI组件引用
    _hpBarTransform = transform.Find("HP Bar");
    _expBarTransform = transform.Find("EXP Bar");
    _levelText = transform.Find("LevelText").GetComponent<Text>();
}

private void UpdateHPBar()
{
    if (_hpBarTransform == null) return;
    // 使用缓存的引用更新UI
    var fill = _hpBarTransform.Find("Fill").GetComponent<Image>();
    // ... 更新逻辑
}
```

- [ ] **Step 5: 测试UI显示和响应性**

Run: 在Unity Editor中运行游戏，测试UI更新
Expected: UI显示正常，响应流畅，无闪烁或延迟

- [ ] **Step 6: 性能对比测试**

Run: 使用Unity Profiler对比优化前后的UI性能
Expected: UI CPU时间减少15-25%

- [ ] **Step 7: Commit**

```bash
git add Assets/GameLevel.scene
git add Assets/Scripts/UI/HUDController.cs
git commit -m "perf: optimize Canvas settings and UI batching"
```

---

### Task 3: 音频对象池实现

**目标:** 实现音频对象池，减少频繁创建/销毁AudioSource的开销

**文件:**
- Create: `Assets/Scripts/Core/AudioPool.cs`
- Modify: `Assets/Scripts/Core/AudioManager.cs`

- [ ] **Step 1: 创建音频对象池**

```csharp
// Assets/Scripts/Core/AudioPool.cs
using UnityEngine;
using System.Collections.Generic;

public class AudioPool : MonoBehaviour
{
    [System.Serializable]
    public class AudioPoolEntry
    {
        public string key;
        public AudioClip clip;
        public int initialSize = 5;
        public int maxSize = 20;
        public float volume = 1f;
        public bool loop = false;
    }

    [SerializeField] private List<AudioPoolEntry> poolEntries = new List<AudioPoolEntry>();
    private Dictionary<string, Queue<AudioSource>> _audioPools = new Dictionary<string, Queue<AudioSource>>();
    private Dictionary<string, AudioPoolEntry> _entryDict = new Dictionary<string, AudioPoolEntry>();
    private Transform _poolRoot;

    private void Awake()
    {
        _poolRoot = transform;
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var entry in poolEntries)
        {
            if (string.IsNullOrEmpty(entry.key) || entry.clip == null) continue;

            _entryDict[entry.key] = entry;
            _audioPools[entry.key] = new Queue<AudioSource>();

            for (int i = 0; i < entry.initialSize; i++)
            {
                CreateAudioSource(entry.key);
            }
        }
    }

    private AudioSource CreateAudioSource(string key)
    {
        var entry = _entryDict[key];
        var audioObj = new GameObject($"Audio_{key}");
        audioObj.transform.SetParent(_poolRoot);

        var source = audioObj.AddComponent<AudioSource>();
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.loop = entry.loop;
        source.playOnAwake = false;
        audioObj.SetActive(false);

        return source;
    }

    public AudioSource Play(string key, Vector3 position)
    {
        if (!_audioPools.ContainsKey(key))
        {
            DebugLogger.LogWarning($"[AudioPool] Key '{key}' not found in pool.");
            return null;
        }

        AudioSource source = GetAudioSource(key);
        if (source == null) return null;

        source.transform.position = position;
        source.gameObject.SetActive(true);
        source.Play();

        if (!source.loop)
        {
            StartCoroutine(ReturnToPoolAfterPlay(source, key));
        }

        return source;
    }

    private AudioSource GetAudioSource(string key)
    {
        var pool = _audioPools[key];
        var entry = _entryDict[key];

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // 检查是否超过最大大小
        int activeCount = CountActiveSources(key);
        if (activeCount < entry.maxSize)
        {
            return CreateAudioSource(key);
        }

        DebugLogger.LogWarning($"[AudioPool] Pool '{key}' at max capacity ({entry.maxSize}). Skipping playback.");
        return null;
    }

    private int CountActiveSources(string key)
    {
        int count = 0;
        foreach (var source in _audioPools[key])
        {
            if (source.gameObject.activeSelf) count++;
        }
        return count;
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, string key)
    {
        yield return new WaitWhile(() => source.isPlaying);
        source.gameObject.SetActive(false);
        _audioPools[key].Enqueue(source);
    }

    public void StopAll(string key)
    {
        if (!_audioPools.ContainsKey(key)) return;

        foreach (var source in _audioPools[key])
        {
            if (source.gameObject.activeSelf)
            {
                source.Stop();
                source.gameObject.SetActive(false);
            }
        }
    }
}
```

- [ ] **Step 2: 修改AudioManager使用音频池**

```csharp
// Assets/Scripts/Core/AudioManager.cs
// 添加音频池引用
private AudioPool _audioPool;

protected override void Awake()
{
    base.Awake();
    _audioPool = GetComponent<AudioPool>();
    // ... 其他初始化代码
}

public void PlaySFX(AudioClip clip)
{
    if (clip == null || _audioPool == null) return;
    string key = clip.name;
    _audioPool.Play(key, transform.position);
}
```

- [ ] **Step 3: 在场景中设置音频池**

在Unity Editor中操作：
1. 选择 `AudioManager` GameObject
2. 添加 `AudioPool` 组件
3. 配置音频池条目(BGM、各种SFX)

- [ ] **Step 4: 测试音频播放**

Run: 在Unity Editor中运行游戏，测试各种音效播放
Expected: 音效播放正常，无卡顿或延迟

- [ ] **Step 5: 性能测试**

Run: 使用Unity Profiler监控AudioSource创建/销毁
Expected: AudioSource创建/销毁操作大幅减少

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/AudioPool.cs
git add Assets/Scripts/Core/AudioManager.cs
git commit -m "perf: implement audio object pooling system"
```

---

### Task 4: 启用GPU Instancing优化精灵渲染

**目标:** 为相同材质的精灵启用GPU Instancing，减少Draw Calls

**文件:**
- Modify: `Assets/Materials/*.mat` (通过Unity编辑器操作)
- Create: `Assets/Scripts/Rendering/GPUInstancingOptimizer.cs`

- [ ] **Step 1: 分析当前材质设置**

Run: 在Unity Project窗口中查看所有材质文件
Expected: 记录当前材质使用的Shader和Instancing设置

- [ ] **Step 2: 为精灵材质启用GPU Instancing**

在Unity Editor中操作：
1. 逐一选择Assets/Materials中的材质文件
2. 在Inspector中找到Shader设置
3. 勾选 `Enable Instancing` 选项
4. 保存材质设置

- [ ] **Step 3: 创建GPU Instancing优化器**

```csharp
// Assets/Scripts/Rendering/GPUInstancingOptimizer.cs
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GPUInstancingOptimizer : MonoBehaviour
{
    [MenuItem("Tools/Optimization/Enable GPU Instancing on All Materials")]
    public static void EnableGPUInstancingOnAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
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
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DebugLogger.Log($"[GPUInstancingOptimizer] Enabled instancing on {enabledCount} materials.");
    }

    [MenuItem("Tools/Optimization/Check Material Instancing Status")]
    public static void CheckMaterialInstancingStatus()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
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
                }
                else
                {
                    DebugLogger.Log($"Material without instancing: {path}");
                }
            }
        }

        DebugLogger.Log($"[GPUInstancingOptimizer] Instancing enabled on {instancingEnabled}/{totalMaterials} materials.");
    }
}
```

- [ ] **Step 4: 运行优化器启用GPU Instancing**

Run: 在Unity Editor中执行 `Tools/Optimization/Enable GPU Instancing on All Materials`
Expected: 所有材质的GPU Instancing被启用

- [ ] **Step 5: 验证GPU Instancing状态**

Run: 执行 `Tools/Optimization/Check Material Instancing Status`
Expected: 显示所有材质的Instancing状态

- [ ] **Step 6: 测试游戏性能**

Run: 在Unity Editor中运行游戏，使用Frame Debugger检查Draw Calls
Expected: Draw Calls数量减少，GPU Instancing生效

- [ ] **Step 7: Commit**

```bash
git add Assets/Materials/
git add Assets/Scripts/Rendering/GPUInstancingOptimizer.cs
git commit -m "perf: enable GPU instancing for sprite materials"
```

---

### Task 5: 优化物理碰撞检测设置

**目标:** 优化物理碰撞检测模式，减少不必要的计算开销

**文件:**
- Modify: `Assets/Prefabs/Enemies/*.prefab` (通过Unity编辑器操作)
- Modify: `Assets/Prefabs/Projectiles/*.prefab` (通过Unity编辑器操作)
- Create: `Assets/Scripts/Physics/CollisionOptimizer.cs`

- [ ] **Step 1: 分析当前碰撞检测设置**

Run: 在Unity Editor中检查主要Prefab的Rigidbody2D和Collider2D设置
Expected: 记录当前碰撞检测模式和碰撞体设置

- [ ] **Step 2: 为低速物体优化碰撞检测**

在Unity Editor中操作：
1. 选择敌人Prefab，修改Rigidbody2D → Collision Detection: `Discrete`
2. 选择掉落物Prefab，修改Rigidbody2D → Collision Detection: `Discrete`
3. 确保非必要物体不使用Continuous检测

- [ ] **Step 3: 为高速物体使用Continuous检测**

在Unity Editor中操作：
1. 选择投射物Prefab，修改Rigidbody2D → Collision Detection: `Continuous`
2. 确保高速移动的物体使用Continuous检测防止穿透

- [ ] **Step 4: 优化碰撞体设置**

在Unity Editor中操作：
1. 为敌人使用CircleCollider2D代替BoxCollider2D(适用时)
2. 减少不必要的碰撞体数量
3. 设置合适的碰撞体大小

- [ ] **Step 5: 创建碰撞优化器工具**

```csharp
// Assets/Scripts/Physics/CollisionOptimizer.cs
using UnityEngine;
using UnityEditor;

public class CollisionOptimizer : EditorWindow
{
    [MenuItem("Tools/Optimization/Collision Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<CollisionOptimizer>("Collision Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Collision Detection Optimization", EditorStyles.boldLabel);

        if (GUILayout.Button("Optimize All Prefabs"))
        {
            OptimizeAllPrefabs();
        }

        if (GUILayout.Button("Analyze Current Settings"))
        {
            AnalyzeCurrentSettings();
        }
    }

    private static void OptimizeAllPrefabs()
    {
        string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
        string[] projectileGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Projectiles" });

        int optimizedCount = 0;

        // 优化敌人碰撞检测
        foreach (string guid in enemyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                var rb2d = prefab.GetComponent<Rigidbody2D>();
                if (rb2d != null && rb2d.bodyType == RigidbodyType2D.Kinematic)
                {
                    rb2d.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                    EditorUtility.SetDirty(prefab);
                    optimizedCount++;
                }
            }
        }

        // 优化投射物碰撞检测
        foreach (string guid in projectileGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                var rb2d = prefab.GetComponent<Rigidbody2D>();
                if (rb2d != null && rb2d.bodyType == RigidbodyType2D.Dynamic)
                {
                    rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    EditorUtility.SetDirty(prefab);
                    optimizedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        DebugLogger.Log($"[CollisionOptimizer] Optimized {optimizedCount} prefabs.");
    }

    private static void AnalyzeCurrentSettings()
    {
        string[] allGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        int discreteCount = 0;
        int continuousCount = 0;
        int totalRigidbodies = 0;

        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                var rb2d = prefab.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    totalRigidbodies++;
                    if (rb2d.collisionDetectionMode == CollisionDetectionMode2D.Discrete)
                    {
                        discreteCount++;
                    }
                    else if (rb2d.collisionDetectionMode == CollisionDetectionMode2D.Continuous)
                    {
                        continuousCount++;
                    }
                }
            }
        }

        DebugLogger.Log($"[CollisionOptimizer] Analysis: {totalRigidbodies} Rigidbody2D found.");
        DebugLogger.Log($"  Discrete: {discreteCount}, Continuous: {continuousCount}");
    }
}
```

- [ ] **Step 6: 测试碰撞检测优化效果**

Run: 在Unity Editor中运行游戏，监控Physics处理时间
Expected: Physics处理时间减少15-20%

- [ ] **Step 7: Commit**

```bash
git add Assets/Prefabs/Enemies/
git add Assets/Prefabs/Projectiles/
git add Assets/Scripts/Physics/CollisionOptimizer.cs
git commit -m "perf: optimize physics collision detection settings"
```

---

## 阶段二：中期优化 (中等收益/中等成本)

### Task 6: 升级到URP渲染管线

**目标:** 升级从Built-in RP到URP，获得现代渲染管线优势

**文件:**
- Create: `Assets/Settings/UniversalRenderPipelineAsset.asset`
- Create: `Assets/Settings/UniversalRenderPipelineAsset_Renderer.asset`
- Modify: `Assets/ProjectSettings/GraphicsSettings.asset`
- Modify: `Assets/ProjectSettings/QualitySettings.asset`
- Modify: `Assets/Materials/*.mat`

- [ ] **Step 1: 安装URP包**

Run: `Window > Package Manager > Universal RP > Install`
Expected: URP包安装成功

- [ ] **Step 2: 创建URP Asset**

在Unity Editor中操作：
1. 右键Project窗口 → Create → Rendering → URP Asset (with Universal Renderer)
2. 命名为 `UniversalRenderPipelineAsset`
3. 配置Render Pipeline设置：
   - Depth Texture: 启用
   - HDR: 启用
   - MSAA: 4x
   - Render Scale: 1.0

- [ ] **Step 3: 设置Graphics Settings**

在Unity Editor中操作：
1. `Edit > Project Settings > Graphics`
2. 将URP Asset拖到 `Scriptable Render Pipeline Settings` 槽位
3. 保存项目设置

- [ ] **Step 4: 转换材质到URP**

在Unity Editor中操作：
1. 选择所有材质文件
2. 使用 `Edit > Render Pipeline > Universal Render Pipeline > Update Project Materials to URP`
3. 检查并修复转换后的材质问题

- [ ] **Step 5: 配置URP Renderer**

在Unity Editor中操作：
1. 选择URP Asset，找到Renderer Data
2. 配置Forward Renderer：
   - 添加Opaques Layer
   - 添加Transparents Layer
   - 启用SRP Batcher
3. 保存设置

- [ ] **Step 6: 测试URP渲染效果**

Run: 在Unity Editor中运行游戏，检查视觉效果
Expected: 游戏视觉效果正常，无渲染错误

- [ ] **Step 7: 性能对比测试**

Run: 使用Unity Profiler对比Built-in RP和URP性能
Expected: GPU性能提升30-50%

- [ ] **Step 8: Commit**

```bash
git add Assets/Settings/
git add Assets/ProjectSettings/GraphicsSettings.asset
git add Assets/ProjectSettings/QualitySettings.asset
git add Assets/Materials/
git commit -m "feat: upgrade to Universal Render Pipeline (URP)"
```

---

### Task 7: 实现Sprite Atlas系统

**目标:** 创建Sprite Atlas系统，减少Draw Calls和内存占用

**文件:**
- Create: `Assets/Sprites/UIAtlas.spriteatlas`
- Create: `Assets/Sprites/CharactersAtlas.spriteatlas`
- Create: `Assets/Sprites/EnemiesAtlas.spriteatlas`
- Create: `Assets/Sprites/ItemsAtlas.spriteatlas`
- Modify: `Assets/Scripts/UI/HUDController.cs`

- [ ] **Step 1: 创建UI Sprite Atlas**

在Unity Editor中操作：
1. 右键Project窗口 → Create → 2D → Sprite Atlas
2. 命名为 `UIAtlas`
3. 在Inspector中：
   - 添加UI文件夹中的所有精灵
   - 设置Atlas Type: `Master`
   - 设置Max Size: `4096`
   - 设置Format: `RGBA 32 bit`

- [ ] **Step 2: 创建角色Sprite Atlas**

在Unity Editor中操作：
1. 创建 `CharactersAtlas`
2. 添加角色精灵
3. 配置压缩格式和图集大小

- [ ] **Step 3: 创建敌人和物品Sprite Atlas**

重复步骤1-2，创建 `EnemiesAtlas` 和 `ItemsAtlas`

- [ ] **Step 4: 配置Atlas包含设置**

在Unity Editor中操作：
1. 为每个Atlas设置Include in Build
2. 配置Packing Settings优化压缩
3. 设置Variant Atlas支持不同分辨率

- [ ] **Step 5: 更新UI代码使用Atlas**

```csharp
// Assets/Scripts/UI/HUDController.cs
// 确保UI代码使用Atlas中的精灵
// Unity会自动处理Atlas引用，无需代码修改
// 但需要验证所有精灵引用正确
```

- [ ] **Step 6: 测试Atlas打包**

Run: 在Unity Editor中点击Atlas的 `Pack Preview` 按钮
Expected: Atlas成功打包，无错误或警告

- [ ] **Step 7: 性能测试**

Run: 使用Frame Debugger检查Draw Calls
Expected: Draw Calls显著减少

- [ ] **Step 8: Commit**

```bash
git add Assets/Sprites/
git commit -m "feat: implement Sprite Atlas system for better batching"
```

---

### Task 8: 扩展LOD系统

**目标:** 为更多游戏对象实现LOD系统，减少远处物体的渲染和更新开销

**文件:**
- Create: `Assets/Scripts/Core/LODManager.cs`
- Modify: `Assets/Scripts/Enemies/EnemyBase.cs`
- Create: `Assets/Scripts/VFX/LODVFX.cs`

- [ ] **Step 1: 创建LOD管理器**

```csharp
// Assets/Scripts/Core/LODManager.cs
using UnityEngine;
using System.Collections.Generic;

public class LODManager : Singleton<LODManager>
{
    public Transform playerTransform;

    [Header("LOD Distances")]
    public float nearDistance = 15f;
    public float mediumDistance = 30f;
    public float farDistance = 50f;

    [Header("LOD Settings")]
    public int nearUpdateInterval = 1;
    public int mediumUpdateInterval = 2;
    public int farUpdateInterval = 5;

    private List<ILodObject> _lodObjects = new List<ILodObject>();

    public enum LODLevel
    {
        Near,
        Medium,
        Far,
        Culled
    }

    public interface ILodObject
    {
        void SetLODLevel(LODLevel level);
        float GetDistanceToPlayer();
    }

    protected override void Awake()
    {
        base.Awake();
        playerTransform = FindObjectOfType<PlayerController>()?.transform;
    }

    public void RegisterLODObject(ILodObject obj)
    {
        if (!_lodObjects.Contains(obj))
        {
            _lodObjects.Add(obj);
        }
    }

    public void UnregisterLODObject(ILodObject obj)
    {
        _lodObjects.Remove(obj);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        foreach (var obj in _lodObjects)
        {
            if (obj == null) continue;

            float distance = obj.GetDistanceToPlayer();
            LODLevel level = CalculateLODLevel(distance);
            obj.SetLODLevel(level);
        }
    }

    private LODLevel CalculateLODLevel(float distance)
    {
        if (distance < nearDistance) return LODLevel.Near;
        if (distance < mediumDistance) return LODLevel.Medium;
        if (distance < farDistance) return LODLevel.Far;
        return LODLevel.Culled;
    }

    public int GetUpdateInterval(LODLevel level)
    {
        switch (level)
        {
            case LODLevel.Near: return nearUpdateInterval;
            case LODLevel.Medium: return mediumUpdateInterval;
            case LODLevel.Far: return farUpdateInterval;
            default: return int.MaxValue;
        }
    }
}
```

- [ ] **Step 2: 扩展EnemyBase支持LOD**

```csharp
// Assets/Scripts/Enemies/EnemyBase.cs
// 实现ILodObject接口
public class EnemyBase : MonoBehaviour, IEnemyTick, LODManager.ILodObject
{
    private LODManager.LODLevel _currentLODLevel = LODManager.LODLevel.Near;

    public void SetLODLevel(LODManager.LODLevel level)
    {
        if (_currentLODLevel == level) return;

        _currentLODLevel = level;

        // 根据LOD级别调整渲染质量
        if (_sr != null)
        {
            switch (level)
            {
                case LODManager.LODLevel.Near:
                    _sr.enabled = true;
                    break;
                case LODManager.LODLevel.Medium:
                    _sr.enabled = true;
                    // 可以降低材质质量
                    break;
                case LODManager.LODLevel.Far:
                    _sr.enabled = true;
                    // 可以使用简化材质
                    break;
                case LODManager.LODLevel.Culled:
                    _sr.enabled = false;
                    break;
            }
        }
    }

    public float GetDistanceToPlayer()
    {
        if (_cachedPlayer == null) return float.MaxValue;
        return Vector2.Distance(transform.position, _cachedPlayer.transform.position);
    }

    // 在Initialize中注册LOD
    public virtual void Initialize(EnemyData data)
    {
        // ... 现有代码 ...
        LODManager.Instance?.RegisterLODObject(this);
    }

    // 在Die或OnDisable中注销LOD
    protected virtual void Die()
    {
        // ... 现有代码 ...
        LODManager.Instance?.UnregisterLODObject(this);
    }
}
```

- [ ] **Step 3: 为VFX创建LOD支持**

```csharp
// Assets/Scripts/VFX/LODVFX.cs
using UnityEngine;

public class LODVFX : MonoBehaviour, LODManager.ILodObject
{
    private ParticleSystem[] _particleSystems;
    private LODManager.LODLevel _currentLODLevel = LODManager.LODLevel.Near;

    private void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    public void SetLODLevel(LODManager.LODLevel level)
    {
        if (_currentLODLevel == level) return;

        _currentLODLevel = level;

        switch (level)
        {
            case LODManager.LODLevel.Near:
                SetParticleEmission(true);
                break;
            case LODManager.LODLevel.Medium:
                SetParticleEmission(true);
                ReduceParticleCount(0.5f);
                break;
            case LODManager.LODLevel.Far:
                SetParticleEmission(true);
                ReduceParticleCount(0.25f);
                break;
            case LODManager.LODLevel.Culled:
                SetParticleEmission(false);
                break;
        }
    }

    public float GetDistanceToPlayer()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.transform.position);
    }

    private void SetParticleEmission(bool enable)
    {
        foreach (var ps in _particleSystems)
        {
            var emission = ps.emission;
            emission.enabled = enable;
        }
    }

    private void ReduceParticleCount(float ratio)
    {
        foreach (var ps in _particleSystems)
        {
            var emission = ps.emission;
            emission.rateOverTime = emission.rateOverTime.constant * ratio;
        }
    }

    private void Start()
    {
        LODManager.Instance?.RegisterLODObject(this);
    }

    private void OnDestroy()
    {
        LODManager.Instance?.UnregisterLODObject(this);
    }
}
```

- [ ] **Step 4: 在场景中添加LODManager**

在Unity Editor中操作：
1. 创建空GameObject，命名为 `LODManager`
2. 添加 `LODManager` 脚本组件
3. 配置LOD距离和更新间隔

- [ ] **Step 5: 测试LOD系统**

Run: 在Unity Editor中运行游戏，观察不同距离下的LOD效果
Expected: 远处物体正确降级，近处物体保持高质量

- [ ] **Step 6: 性能测试**

Run: 使用Unity Profiler监控渲染和更新性能
Expected: 渲染和更新开销随距离有效减少

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Core/LODManager.cs
git add Assets/Scripts/Enemies/EnemyBase.cs
git add Assets/Scripts/VFX/LODVFX.cs
git commit -m "feat: implement extended LOD system for better performance"
```

---

### Task 9: 优化纹理和资源加载

**目标:** 优化纹理设置和资源加载策略，减少内存占用和加载时间

**文件:**
- Modify: `Assets/Textures/**/*.png` (通过Unity编辑器操作)
- Create: `Assets/Scripts/Resources/TextureOptimizer.cs`
- Modify: `Assets/Scripts/Core/AudioManager.cs`

- [ ] **Step 1: 分析当前纹理设置**

Run: 在Unity Editor中选择纹理文件，检查Import Settings
Expected: 记录当前纹理格式、压缩设置和分辨率

- [ ] **Step 2: 优化UI纹理**

在Unity Editor中操作：
1. 选择UI纹理
2. 修改Import Settings：
   - Max Size: `512` 或 `1024`
   - Compression: `Normal Quality`
   - Format: `ASTC 6x6` (移动平台) 或 `RGBA 32 bit` (PC)

- [ ] **Step 3: 优化游戏纹理**

在Unity Editor中操作：
1. 选择游戏纹理(角色、敌人、物品)
2. 修改Import Settings：
   - Max Size: `2048`
   - Generate Mip Maps: 启用
   - Compression: `High Quality`

- [ ] **Step 4: 创建纹理优化工具**

```csharp
// Assets/Scripts/Resources/TextureOptimizer.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureOptimizer : EditorWindow
{
    [MenuItem("Tools/Optimization/Texture Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizer>("Texture Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Optimization", EditorStyles.boldLabel);

        if (GUILayout.Button("Optimize UI Textures"))
        {
            OptimizeUITextures();
        }

        if (GUILayout.Button("Optimize Game Textures"))
        {
            OptimizeGameTextures();
        }

        if (GUILayout.Button("Analyze Texture Memory"))
        {
            AnalyzeTextureMemory();
        }
    }

    private static void OptimizeUITextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/UI" });
        int optimizedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
                optimizedCount++;
            }
        }

        DebugLogger.Log($"[TextureOptimizer] Optimized {optimizedCount} UI textures.");
    }

    private static void OptimizeGameTextures()
    {
        string[] gamePaths = new[]
        {
            "Assets/Textures/Characters",
            "Assets/Textures/Enemies",
            "Assets/Textures/Items"
        };

        int optimizedCount = 0;

        foreach (string folder in gamePaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    importer.maxTextureSize = 2048;
                    importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.CompressedHighQuality;
                    importer.SaveAndReimport();
                    optimizedCount++;
                }
            }
        }

        DebugLogger.Log($"[TextureOptimizer] Optimized {optimizedCount} game textures.");
    }

    private static void AnalyzeTextureMemory()
    {
        string[] allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures" });
        long totalMemory = 0;
        int textureCount = 0;

        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture != null)
            {
                int memorySize = Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
                totalMemory += memorySize;
                textureCount++;

                if (memorySize > 1024 * 1024) // 大于1MB
                {
                    DebugLogger.Log($"Large texture: {path} - {memorySize / 1024}KB");
                }
            }
        }

        DebugLogger.Log($"[TextureOptimizer] Total texture memory: {totalMemory / 1024}KB ({textureCount} textures)");
    }
}
```

- [ ] **Step 5: 优化音频加载**

```csharp
// Assets/Scripts/Core/AudioManager.cs
// 添加异步音频加载
private void LoadAudioClipsAsync()
{
    StartCoroutine(LoadAudioClipsCoroutine());
}

private System.Collections.IEnumerator LoadAudioClipsCoroutine()
{
    // 异步加载音频资源
    ResourceRequest bgmRequest = Resources.LoadAsync<AudioClip>("Audio/BGM/BattleTheme");
    yield return bgmRequest;

    if (bgmRequest.asset != null)
    {
        _bgmClip = bgmRequest.asset as AudioClip;
    }

    // 加载其他音频...
}
```

- [ ] **Step 6: 测试资源加载性能**

Run: 在Unity Editor中运行游戏，监控资源加载时间和内存占用
Expected: 资源加载时间减少，内存占用优化

- [ ] **Step 7: Commit**

```bash
git add Assets/Textures/
git add Assets/Scripts/Resources/TextureOptimizer.cs
git add Assets/Scripts/Core/AudioManager.cs
git commit -m "perf: optimize texture settings and resource loading"
```

---

## 阶段三：长期架构升级 (高收益/高成本)

### Task 10: 实现多线程敌人AI处理

**目标:** 使用多线程处理敌人AI逻辑，减轻主线程负担

**文件:**
- Create: `Assets/Scripts/Core/ThreadedAIProcessor.cs`
- Modify: `Assets/Scripts/Enemies/EnemyBase.cs`
- Modify: `Assets/Scripts/Enemies/EnemyManager.cs`

- [ ] **Step 1: 创建线程化AI处理器**

```csharp
// Assets/Scripts/Core/ThreadedAIProcessor.cs
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;

public class ThreadedAIProcessor : MonoBehaviour
{
    private ConcurrentQueue<AITask> _taskQueue = new ConcurrentQueue<AITask>();
    private Thread _workerThread;
    private volatile bool _isRunning = true;

    public struct AITask
    {
        public int enemyId;
        public Vector2 enemyPosition;
        public Vector2 playerPosition;
        public float moveSpeed;
        public Vector2 resultDirection;
    }

    private void Start()
    {
        _workerThread = new Thread(ProcessAITasks);
        _workerThread.IsBackground = true;
        _workerThread.Start();
    }

    private void ProcessAITasks()
    {
        while (_isRunning)
        {
            if (_taskQueue.TryDequeue(out AITask task))
            {
                // 在后台线程计算AI逻辑
                Vector2 direction = (task.playerPosition - task.enemyPosition).normalized;
                task.resultDirection = direction;

                // 将结果存回主线程
                ExecuteOnMainThread(() =>
                {
                    var enemy = FindEnemyById(task.enemyId);
                    if (enemy != null)
                    {
                        enemy.SetAIDirection(task.resultDirection);
                    }
                });
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }

    private EnemyBase FindEnemyById(int id)
    {
        // 实现根据ID查找敌人的逻辑
        return null;
    }

    private void ExecuteOnMainThread(System.Action action)
    {
        // 使用Unity的主线程调度器
        UnityMainThreadDispatcher.Instance.Enqueue(action);
    }

    public void EnqueueAITask(AITask task)
    {
        _taskQueue.Enqueue(task);
    }

    private void OnDestroy()
    {
        _isRunning = false;
        if (_workerThread != null && _workerThread.IsAlive)
        {
            _workerThread.Join();
        }
    }
}

// 主线程调度器
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly System.Collections.Generic.Queue<System.Action> _executionQueue =
        new System.Collections.Generic.Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
```

- [ ] **Step 2: 修改EnemyBase支持线程化AI**

```csharp
// Assets/Scripts/Enemies/EnemyBase.cs
private Vector2 _aiDirection = Vector2.zero;
private bool _hasAIResult = false;

public void SetAIDirection(Vector2 direction)
{
    _aiDirection = direction;
    _hasAIResult = true;
}

public virtual void OnFixedTick(float dt)
{
    if (_cachedPlayer == null) return;

    // 使用多线程AI结果
    Vector2 moveDirection;
    if (_hasAIResult)
    {
        moveDirection = _aiDirection;
        _hasAIResult = false;
    }
    else
    {
        // 回退到同步计算
        moveDirection = ((Vector2)_cachedPlayer.transform.position - (Vector2)transform.position).normalized;
    }

    // ... 现有移动逻辑 ...
}
```

- [ ] **Step 3: 修改EnemyManager集成线程化AI**

```csharp
// Assets/Scripts/Enemies/EnemyManager.cs
private ThreadedAIProcessor _aiProcessor;

private void Start()
{
    _playerStats = FindObjectOfType<PlayerStats>();
    _aiProcessor = FindObjectOfType<ThreadedAIProcessor>();
}

private void FixedUpdate()
{
    // 将AI任务提交到线程化处理器
    if (_aiProcessor != null)
    {
        foreach (var enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy != null && _cachedPlayer != null)
            {
                var task = new ThreadedAIProcessor.AITask
                {
                    enemyId = enemy.GetInstanceID(),
                    enemyPosition = enemy.transform.position,
                    playerPosition = _cachedPlayer.transform.position,
                    moveSpeed = enemy.Data.moveSpeed
                };
                _aiProcessor.EnqueueAITask(task);
            }
        }
    }

    // ... 现有FixedUpdate逻辑 ...
}
```

- [ ] **Step 4: 测试多线程AI**

Run: 在Unity Editor中运行游戏，监控CPU线程使用情况
Expected: AI计算分布在多个线程，主线程负载减轻

- [ ] **Step 5: 性能测试**

Run: 使用Unity Profiler对比单线程和多线程性能
Expected: 主线程CPU时间减少20-30%

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/ThreadedAIProcessor.cs
git add Assets/Scripts/Enemies/EnemyBase.cs
git add Assets/Scripts/Enemies/EnemyManager.cs
git commit -m "feat: implement multi-threaded enemy AI processing"
```

---

### Task 11: 核心游戏逻辑迁移到ECS

**目标:** 将核心游戏逻辑迁移到Unity ECS架构，获得极致性能

**文件:**
- Create: `Assets/Scripts/ECS/Components/EnemyComponent.cs`
- Create: `Assets/Scripts/ECS/Components/PlayerComponent.cs`
- Create: `Assets/Scripts/ECS/Systems/EnemyMovementSystem.cs`
- Create: `Assets/Scripts/ECS/Systems/CombatSystem.cs`
- Create: `Assets/Scripts/ECS/Authoring/EnemyAuthoring.cs`

- [ ] **Step 1: 安装ECS包**

Run: `Window > Package Manager > Entities > Install`
Expected: ECS相关包安装成功

- [ ] **Step 2: 创建ECS组件**

```csharp
// Assets/Scripts/ECS/Components/EnemyComponent.cs
using Unity.Entities;

public struct EnemyComponent : IComponentData
{
    public float moveSpeed;
    public float currentHP;
    public float maxHP;
    public int damage;
    public bool isElite;
}

public struct TargetComponent : IComponentData
{
    public Entity targetEntity;
    public float targetDistance;
}
```

```csharp
// Assets/Scripts/ECS/Components/PlayerComponent.cs
using Unity.Entities;
using Unity.Mathematics;

public struct PlayerComponent : IComponentData
{
    public float moveSpeed;
    public float currentHP;
    public float maxHP;
    public float3 position;
}
```

- [ ] **Step 3: 创建ECS系统**

```csharp
// Assets/Scripts/ECS/Systems/EnemyMovementSystem.cs
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class EnemyMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .ForEach((ref Translation translation,
                     in EnemyComponent enemy,
                     in TargetComponent target) =>
            {
                if (target.targetEntity == Entity.Null) return;

                float3 targetPos = float3.zero;
                if (HasComponent<Translation>(target.targetEntity))
                {
                    targetPos = GetComponent<Translation>(target.targetEntity).Value;
                }

                float3 direction = math.normalize(targetPos - translation.Value);
                translation.Value += direction * enemy.moveSpeed * deltaTime;
            })
            .ScheduleParallel();
    }
}
```

```csharp
// Assets/Scripts/ECS/Systems/CombatSystem.cs
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ref EnemyComponent enemy,
                     in Translation translation,
                     in LocalToWorld localToWorld) =>
            {
                // 处理战斗逻辑
                float3 position = translation.Value;

                // 检测与玩家的碰撞
                // ... 战斗逻辑 ...
            })
            .ScheduleParallel();
    }
}
```

- [ ] **Step 4: 创建Authoring组件**

```csharp
// Assets/Scripts/ECS/Authoring/EnemyAuthoring.cs
using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float moveSpeed = 2f;
    public float maxHP = 100f;
    public int damage = 10;
    public bool isElite = false;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new EnemyComponent
        {
            moveSpeed = moveSpeed,
            currentHP = maxHP,
            maxHP = maxHP,
            damage = damage,
            isElite = isElite
        });

        dstManager.AddComponentData(entity, new TargetComponent());
    }
}
```

- [ ] **Step 5: 集成ECS到现有游戏**

创建桥接代码，让ECS和传统MonoBehaviour系统协同工作

- [ ] **Step 6: 测试ECS系统**

Run: 在Unity Editor中运行游戏，验证ECS系统正常工作
Expected: ECS驱动的游戏逻辑正常运行

- [ ] **Step 7: 性能测试**

Run: 使用Unity Profiler对比ECS和MonoBehaviour性能
Expected: ECS版本性能提升50-100%

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/ECS/
git commit -m "feat: migrate core gameplay to Unity ECS architecture"
```

---

### Task 12: 自定义渲染管线优化

**目标:** 创建自定义渲染管线，针对2D游戏进行极致优化

**文件:**
- Create: `Assets/Scripts/Rendering/Custom2DRenderPipeline.cs`
- Create: `Assets/Scripts/Rendering/Custom2DRenderPipelineAsset.cs`
- Modify: `Assets/ProjectSettings/GraphicsSettings.asset`

- [ ] **Step 1: 创建自定义渲染管线Asset**

```csharp
// Assets/Scripts/Rendering/Custom2DRenderPipelineAsset.cs
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "Custom2DRenderPipelineAsset", menuName = "Rendering/Custom2D Render Pipeline")]
public class Custom2DRenderPipelineAsset : RenderPipelineAsset
{
    public int renderScale = 1;
    public bool enableSRPBatcher = true;
    public bool enableHDR = false;
    public int msaaSampleCount = 1;

    protected override RenderPipeline CreatePipeline()
    {
        return new Custom2DRenderPipeline(this);
    }
}
```

- [ ] **Step 2: 实现自定义渲染管线**

```csharp
// Assets/Scripts/Rendering/Custom2DRenderPipeline.cs
using UnityEngine;
using UnityEngine.Rendering;

public class Custom2DRenderPipeline : RenderPipeline
{
    private Custom2DRenderPipelineAsset _asset;

    public Custom2DRenderPipeline(Custom2DRenderPipelineAsset asset)
    {
        _asset = asset;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            RenderCamera(context, camera);
        }
    }

    private void RenderCamera(ScriptableRenderContext context, Camera camera)
    {
        // 设置渲染目标
        var cmd = CommandBufferPool.Get("Custom2DRender");
        cmd.Clear();

        // 设置相机参数
        context.SetupCameraProperties(camera);

        // 剔除
        ScriptableCullingParameters cullingParameters;
        if (!camera.TryGetCullingParameters(out cullingParameters))
        {
            CommandBufferPool.Release(cmd);
            return;
        }

        var cullingResults = context.Cull(ref cullingParameters);

        // 设置渲染目标
        cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        cmd.ClearRenderTarget(true, true, camera.backgroundColor);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 绘制不透明物体
        DrawingSettings drawingSettings = CreateDrawingSettings(camera);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // 提交绘制命令
        context.Submit();
        CommandBufferPool.Release(cmd);
    }

    private DrawingSettings CreateDrawingSettings(Camera camera)
    {
        var sortingCriteria = camera.opaqueSortMode == Camera.OpaqueSortMode.Default
            ? SortingCriteria.CommonOpaque
            : SortingCriteria.Quadratic;

        DrawingSettings settings = new DrawingSettings(
            new ShaderTagId("UniversalForward"),
            sortingCriteria)
        {
            enableInstancing = true,
            enableDynamicBatching = false
        };

        return settings;
    }
}
```

- [ ] **Step 3: 配置项目使用自定义渲染管线**

在Unity Editor中操作：
1. 创建Custom2DRenderPipelineAsset实例
2. 在Graphics Settings中设置为当前渲染管线
3. 配置渲染管线参数

- [ ] **Step 4: 测试自定义渲染管线**

Run: 在Unity Editor中运行游戏，检查渲染效果
Expected: 渲染效果正常，性能提升

- [ ] **Step 5: 性能测试**

Run: 使用Unity Profiler对比不同渲染管线性能
Expected: 自定义渲染管线性能最优

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Rendering/
git add Assets/ProjectSettings/GraphicsSettings.asset
git commit -m "feat: implement custom 2D render pipeline for optimal performance"
```

---

## 总结和验证

### Task 13: 综合性能测试和验证

**目标:** 对所有优化进行综合测试，验证性能提升效果

**文件:**
- Create: `Assets/Scripts/Editor/PerformanceBenchmark.cs`
- Create: `docs/performance-report-optimization.md`

- [ ] **Step 1: 创建性能基准测试工具**

```csharp
// Assets/Scripts/Editor/PerformanceBenchmark.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PerformanceBenchmark : EditorWindow
{
    [MenuItem("Tools/Performance/Run Full Benchmark")]
    public static void RunFullBenchmark()
    {
        GetWindow<PerformanceBenchmark>("Performance Benchmark").RunBenchmark();
    }

    private List<BenchmarkResult> _results = new List<BenchmarkResult>();

    public struct BenchmarkResult
    {
        public string testName;
        public float avgFPS;
        public float minFPS;
        public float maxFPS;
        public float frameTime;
        public int drawCalls;
        public long memoryUsage;
    }

    public void RunBenchmark()
    {
        _results.Clear();

        // 运行各个测试场景
        RunBenchmarkScene("MainMenu", "Main Menu Performance");
        RunBenchmarkScene("GameLevel", "Gameplay Performance");
        RunBenchmarkScene("GameLevel", "Heavy Combat Performance", true);

        // 生成报告
        GenerateReport();
    }

    private void RunBenchmarkScene(string sceneName, string testName, bool heavyLoad = false)
    {
        // 加载场景
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/{sceneName}.unity");

        // 运行测试
        var result = new BenchmarkResult
        {
            testName = testName
        };

        // 模拟游戏运行和收集性能数据
        // ... 性能数据收集逻辑 ...

        _results.Add(result);
    }

    private void GenerateReport()
    {
        string reportPath = "docs/performance-report-optimization.md";
        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("# 性能优化报告");
            writer.WriteLine($"生成时间: {System.DateTime.Now}");
            writer.WriteLine();

            foreach (var result in _results)
            {
                writer.WriteLine($"## {result.testName}");
                writer.WriteLine($"- 平均FPS: {result.avgFPS:F1}");
                writer.WriteLine($"- 帧时间: {result.frameTime:F2}ms");
                writer.WriteLine($"- Draw Calls: {result.drawCalls}");
                writer.WriteLine($"- 内存使用: {result.memoryUsage / 1024}KB");
                writer.WriteLine();
            }
        }

        DebugLogger.Log($"[PerformanceBenchmark] Report generated: {reportPath}");
    }
}
```

- [ ] **Step 2: 运行优化前基准测试**

Run: 执行 `Tools/Performance/Run Full Benchmark` 记录优化前性能
Expected: 生成优化前性能报告

- [ ] **Step 3: 分阶段实施优化**

按照Task 1-12的顺序实施各阶段优化

- [ ] **Step 4: 运行优化后性能测试**

Run: 再次执行 `Tools/Performance/Run Full Benchmark`
Expected: 生成优化后性能报告

- [ ] **Step 5: 对比分析性能提升**

对比优化前后的性能数据，计算提升百分比

- [ ] **Step 6: 生成最终优化报告**

创建详细的优化报告，包括：
- 各阶段优化效果
- 性能提升数据
- 遇到的问题和解决方案
- 下一步优化建议

- [ ] **Step 7: Commit最终报告**

```bash
git add docs/performance-report-optimization.md
git commit -m "docs: add comprehensive performance optimization report"
```

---

## 执行计划

### 预期时间安排
- **阶段一（立即实施）**: 1-2周
- **阶段二（中期优化）**: 2-3周  
- **阶段三（长期架构升级）**: 4-6周

### 预期性能提升
- **阶段一**: 20-30% 整体性能提升
- **阶段二**: 额外 30-40% 性能提升
- **阶段三**: 额外 50-100% 性能提升

### 风险和注意事项
1. **兼容性风险**: URP升级可能影响现有材质和着色器
2. **开发复杂度**: ECS架构学习曲线较陡
3. **测试成本**: 每个阶段需要充分测试确保游戏功能正常

### 成功标准
- 游戏在目标设备上稳定达到60 FPS
- 内存占用控制在合理范围内
- 优化后游戏体验无明显下降
- 代码质量和可维护性保持良好水平