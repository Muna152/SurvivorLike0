# UI自动导航系统使用指南

## 概述

UI自动导航系统（AutoUIManager）为游戏提供了完全自动化的UI导航功能，配合现有的Auto-Pilot系统，可以实现从主菜单到游戏结束的完全自动化测试。

## 功能特性

### 自动导航场景

| UI界面 | 触发条件 | 自动操作 | 延迟时间 |
|--------|----------|----------|----------|
| 主菜单 | Auto-Pilot开启 + 界面显示 | 自动选择存档1（如无激活）<br>自动点击"开始游戏" | 0.5秒 |
| 角色选择 | Auto-Pilot开启 + 界面显示 | 自动选择第一个已解锁角色<br>自动点击"开始战斗" | 0.5秒 |
| 结算界面 | Auto-Pilot开启 + 界面显示 | 自动点击"再来一局" | 1.0秒 |
| 宝箱开箱 | Auto-Pilot开启 + 动画结束 | 无需操作（UpgradeManager已处理） | - |

### 系统架构

```
AutoUIManager (Singleton)
├── 订阅 GameEvents.OnAutoPilotToggled
├── 订阅 GameManager.OnGameStateChanged
├── 各UI界面的自动导航逻辑
│   ├── MainMenuUI 自动存档选择 + 开始游戏
│   ├── CharacterSelectUI 自动角色选择 + 开始战斗
│   └── ResultScreen 自动再来一局
└── 延迟机制确保UI动画完成后再执行
```

## 使用方法

### 1. 启用Auto-Pilot

在游戏中按 `P` 键启用Auto-Pilot，这将同时启用：
- 玩家自动移动（PlayerController）
- 自动升级选择（UpgradeManager）
- UI自动导航（AutoUIManager）

### 2. 完全自动化测试流程

启用Auto-Pilot后，系统将自动执行以下流程：

```
主菜单
  ↓ 自动选择存档
  ↓ 自动点击"开始游戏"
角色选择界面
  ↓ 自动选择第一个已解锁角色
  ↓ 自动点击"开始战斗"
游戏进行中（自动移动 + 自动攻击 + 自动升级）
  ↓ 玩家死亡/胜利
结算界面
  ↓ 自动点击"再来一局"
  ↓ 返回主菜单
  ↓ 重复上述流程
```

### 3. 手动触发导航

如果需要在特定场景手动触发导航，可以使用：

```csharp
// 在主菜单中
if (AutoUIManager.HasInstance)
    AutoUIManager.Instance.TriggerNavigationCheck();

// 在角色选择界面中
if (AutoUIManager.HasInstance)
    AutoUIManager.Instance.NavigateCharacterSelect();

// 在结算界面中
if (AutoUIManager.HasInstance)
    AutoUIManager.Instance.NavigateResultScreen();
```

## 配置选项

AutoUIManager提供以下可配置延迟时间（在Inspector中调整）：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Menu Navigation Delay` | 0.5秒 | 主菜单导航延迟 |
| `Character Select Delay` | 0.5秒 | 角色选择导航延迟 |
| `Result Screen Delay` | 1.0秒 | 结算界面导航延迟 |

这些延迟时间确保UI动画完成后再执行自动操作。

## 调试

### 日志输出

AutoUIManager会输出详细的调试日志，方便追踪自动导航行为：

```
[AutoUI] Auto-Pilot enabled, checking for navigation opportunities...
[AutoUI] Game state changed to: Menu
[AutoUI] Starting main menu navigation...
[AutoUI] Selected save slot 0: 存档名1
[AutoUI] Clicking 'Start Game' button...
[AutoUI] Starting character select navigation...
[AutoUI] Selected character: 勇者
[AutoUI] 'Start Battle' clicked
[AutoUI] Starting result screen navigation...
[AutoUI] Clicking 'Retry' button...
[AutoUI] 'Retry' clicked
```

### 禁用自动导航

如果需要临时禁用自动导航（保留Auto-Pilot的游戏内功能），可以：

1. 按P键禁用Auto-Pilot（同时禁用所有自动功能）
2. 修改AutoUIManager代码，注释掉相关导航逻辑

## 技术实现细节

### 事件驱动架构

系统通过订阅以下事件实现自动导航：

1. **GameEvents.OnAutoPilotToggled** - Auto-Pilot状态变化时触发导航检查
2. **GameManager.OnGameStateChanged** - 游戏状态变化时触发导航检查
3. **UI显示事件** - 各UI界面在Show()时通知AutoUIManager

### 延迟执行机制

使用协程确保UI动画完成后再执行操作：

```csharp
private IEnumerator NavigateMainMenuCoroutine()
{
    yield return new WaitForSecondsRealtime(_menuNavigationDelay);
    // 执行自动导航操作
}
```

### 单例持久化

AutoUIManager继承自Singleton<T>，并使用DontDestroyOnLoad确保跨场景持久化：

```csharp
protected override void Awake()
{
    base.Awake();
    DontDestroyOnLoad(gameObject);
}
```

## 扩展性

系统设计为可扩展，支持未来新增UI界面的自动导航：

1. 在目标UI类的Show()方法中调用 `AutoUIManager.Instance.TriggerNavigationCheck()`
2. 在AutoUIManager中添加对应的导航方法
3. 使用协程实现延迟执行逻辑

### 示例：添加新UI的自动导航

```csharp
// 1. 在新UI类的Show()方法中
public void Show()
{
    // ... 显示UI的代码 ...

    // 通知AutoUIManager
    if (AutoUIManager.HasInstance)
        AutoUIManager.Instance.NavigateNewUI();
}

// 2. 在AutoUIManager中添加导航方法
public void NavigateNewUI()
{
    if (!PlayerController.IsAutoPilot) return;

    if (_pendingNavigation != null)
        StopCoroutine(_pendingNavigation);

    _pendingNavigation = StartCoroutine(NavigateNewUICoroutine());
}

private IEnumerator NavigateNewUICoroutine()
{
    yield return new WaitForSecondsRealtime(0.3f);

    var newUI = FindObjectOfType<NewUI>();
    if (newUI != null)
    {
        // 执行自动导航操作
        newUI.AutoClickButton();
    }

    _pendingNavigation = null;
}
```

## 注意事项

1. **依赖关系**：UI自动导航依赖于Auto-Pilot状态，只有当Auto-Pilot启用时才会执行
2. **延迟时间**：如果UI动画较慢，可能需要增加延迟时间配置
3. **错误处理**：系统会在找不到目标UI时输出警告日志，不会导致崩溃
4. **手动干预**：任何时候按P键禁用Auto-Pilot都会取消所有待执行的自动导航

## 最佳实践

1. **测试自动化**：使用Auto-Pilot + UI自动导航进行长时间稳定性测试
2. **数值平衡**：观察自动导航下的游戏流程，验证难度曲线是否合理
3. **性能监控**：在自动导航模式下监控游戏性能，确保长时间运行稳定
4. **日志分析**：收集自动导航日志，分析系统行为是否符合预期

## 已知限制

1. **暂停菜单**：暂停菜单不提供自动导航（因为暂停通常是手动操作）
2. **商店界面**：商店界面不提供自动导航（需要玩家手动选择升级）
3. **图鉴界面**：图鉴界面不提供自动导航（纯信息展示）

---

*文档版本: v1.0 | 创建日期: 2026-06-10*
