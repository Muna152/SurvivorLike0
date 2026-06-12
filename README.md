# 暗夜幸存者 (Night Survivors)

> 玩家操控角色在黑暗中生存，武器自动攻击海量敌人，通过升级获取更强大的武器和道具，活到最后即为胜利。

## 游戏信息

| 属性 | 说明 |
|------|------|
| **类型** | Roguelite 生存动作游戏 |
| **参考** | Vampire Survivors / Brotato / Holocure |
| **视角** | 俯视角 2D |
| **核心玩法** | 移动躲避 + 自动攻击 + 升级选择 |
| **单局时长** | 15~30 分钟 |
| **美术风格** | 像素风 / 扁平化卡通 |

## 技术栈

- **引擎**: Unity 2022.3 (Tuanjie)
- **渲染管线**: URP

## 项目结构

```
Assets/
├── Animators/       # 动画控制器
├── Art/             # 美术资源
├── Audio/           # 音频（BGM / SFX）
├── Data/            # ScriptableObject 数据资产
├── Editor/          # 编辑器扩展
├── Fonts/           # 字体
├── Prefabs/         # 预制体
├── Resources/       # 运行时加载资源
├── Scenes/          # 场景
├── Scripts/         # C# 脚本
│   ├── Core/        # 核心系统（游戏状态、事件等）
│   ├── Player/      # 玩家逻辑
│   ├── Enemies/     # 敌人 AI 与生成
│   ├── Weapons/     # 武器系统
│   ├── Upgrades/    # 升级与技能选择
│   ├── Drops/       # 掉落物（经验宝石等）
│   ├── UI/          # 界面逻辑
│   ├── Rendering/   # 渲染相关
│   ├── VFX/         # 视觉特效
│   └── Data/        # 数据定义
├── Sprites/         # 2D 精灵图
└── Tests/           # 测试
```

## 快速开始

1. 克隆仓库
   ```bash
   git clone https://github.com/Muna152/SurvivorLike0.git
   ```
2. 用 Unity 2022.3 打开项目
3. 打开 `Assets/Scenes/GameLevel.unity`
4. 点击 Play 运行

## 文档

- [游戏设计文档 (GDD)](Docs/GDD.md)
- [技术设计文档 (TDD)](Docs/TDD.md)

---

## Vibe Coding 开发经验

本项目全程使用 **Codely CLI + Unity** 进行 Vibe Coding 开发，从零到可玩仅用数天。以下是实战中总结的关键经验。

### 🏗️ 架构与设计

1. **数据驱动优于硬编码** — 将游戏参数（敌人生成间隔、批次大小、经验曲线等）提取到 `ScriptableObject`（如 `GameBalanceConfig`），使数值调整无需改代码、无需重新编译，策划和 AI 都能快速迭代。
2. **集中式更新循环** — 用 `IEnemyTick` 接口 + 中央 `TickManager` 替代每个敌人各自 `Update()`，将数百个 `MonoBehaviour.Update()` 归一为单次遍历，CPU 缓存命中率大幅提升。
3. **事件总线解耦** — `GameEvents` 静态事件系统让武器、UI、掉落等模块松耦合，升级/击杀/拾取等事件无需互相引用，AI 新增功能时只需订阅事件即可。

### ⚡ 性能优化

4. **先 Profile 再优化** — 不要凭猜测优化。用 Unity Profiler 定位热点后，发现 `Debug.Log` 的字符串分配和 I/O 是主要开销之一。用 `Conditional` 编译的 `DebugLogger` 替代后，Release 构建零开销。
5. **对象池是刚需** — 对于频繁创建销毁的对象（子弹、敌人、掉落物、音效源），对象池比 `Instantiate/Destroy` 快一个数量级。注意池的预热（Pre-warm）和注册时序，避免空引用。
6. **SpatialGrid 替代 Physics.Overlap** — 对 500+ 敌人的范围查询，自建网格空间分区比 Unity 物理查询快数倍，且避免了 Collider 同步开销。
7. **消除每帧 GC 分配** — `Vector3.magnitude`（内部 `sqrt`）、`ToString()`、`List.ForEach` 闭包都是隐形 GC 杀手。用 `sqrMagnitude` 比较距离、缓存字符串、避免闭包，让 Profiler 上的 GC Alloc 列归零。
8. **GPU Instancing 一键收益** — 对使用相同材质的 Sprite，开启 `GPU Instancing` 后 Draw Call 大幅下降，改动仅勾选一个复选框。

### 🐛 调试与修复

9. **状态泄漏是隐蔽 Bug 的头号来源** — "重试后上个角色的武器出现在新角色身上"这类 Bug，根因是 `static` 变量（如 `SelectedCharacter`）在场景重载后未清除。用 `[RuntimeInitializeOnLoadMethod]` 或显式 Reset 兜底。
10. **对象池注册竞态** — `EnemySpawner` 在 `Awake` 注册池、`Start` 生成敌人，但如果池未就绪就会空引用。解法：Lazy Init + 防御性空检查。
11. **UI 键盘导航干扰游戏操作** — Unity UI 的 `Selectable` 默认响应方向键，会劫持 WASD 输入。全局禁用 `navigation = Navigation.Mode.None` 解决。
12. **升级选择 UI 的引用断裂** — `ScriptableObject` 重构后，运行时按 ID 查找替代直接引用，避免资产移动/重命名导致的 `Missing Reference`。

### 🤖 AI 协作开发

13. **分阶段交付优于一次性大需求** — 将 MVP 拆为 Phase 1~4，每阶段有明确可验证的交付物（核心循环→武器扩展→地图/UI→打磨），AI 每完成一个 Phase 即可验证，避免"一步错步步错"。
14. **编译门禁是 AI 开发的安全网** — 每次代码改动后立即编译 + 检查 Console，0 error 才继续。AI 容易引入编译错误（命名空间、引用缺失），门禁机制将问题拦截在单步之内。
15. **让 AI 写计划而非直接写代码** — 用 `writing-plans` skill 生成结构化实施计划（含文件列表、改动步骤、验收标准），再让 AI 按计划逐步执行，比"直接开写"减少 50% 以上的返工。
16. **ScriptableObject 是 AI 的最佳数据接口** — AI 修改 `.cs` 中的默认值不如直接改 SO 资产直观且安全；SO 参数化后，AI 可以只调数字不改逻辑，大幅降低引入 Bug 的风险。

### 📦 工程与 Git

17. **`.gitignore` 要从一开始就写好** — `Library/`、`obj/`、`.slnx`、`profiler_log.raw`、`.codegraph/` 等文件一旦入库，清理历史非常痛苦（`filter-branch` + `gc --aggressive`）。
18. **大文件是 GitHub 推送的定时炸弹** — `profiler_log.raw`（1.18GB）导致推送被拒，用 `filter-branch` 从历史清除 + `refs/original/` 残留引用清理 + `gc --prune=now` 才彻底回收空间（375 MiB → 110 MiB）。
19. **匿名化提交需要重写全部历史** — 单改 `user.name/email` 只影响新提交；已有提交需 `filter-branch --env-filter` 重写，记得清除 `refs/original/` 否则 gc 不回收旧对象。

## License

All rights reserved.
