# AI 可读文档体系规范

> ⚠️ **状态说明**：本文档描述的是 Docs 的目标目录结构（`gdd/`、`tdd/`、`feature-specs/`、`exec-plans/` 等）。
> **当前 Docs 仍为扁平单文件结构**（GDD.md、TDD.md、ROADMAP.md、TASKS.md），尚未按此规范拆分。
> 拆分工作属于未来重构任务，不属于当前迭代范围。
>
> 本文档是 AI 智能体开发时必须遵守的文档结构与读写规范。
> 来源：DOCUMENTATION_GUIDE.md §10，本文档可独立使用。

---

## 1. 核心原则

1. **渐进式披露** — AI 从小入口开始，按需深入，不被信息淹没
2. **代码仓库即真相** — 仓库外的信息对 AI 等于不存在
3. **结构化+可索引** — 文件名和目录结构本身就是导航系统
4. **交叉链接** — 文档之间互相引用，形成知识网络
5. **按需加载** — AI 只读当前任务相关的文件，杜绝一次性加载全部文档
6. **去重** — 同一条信息只在一个层级写完整，其他层级只引用

> **给 AI 一张地图，不是一本 1000 页的说明书。**

---

## 2. 目录结构

```
Docs/
├── AGENTS.md                        # 🗺️ AI 唯一入口（<100行）
├── CORE_BELIEFS.md                  # ⚖️ 项目宪法（不可违反的原则，<50行）
├── ARCHITECTURE.md                  # 🏗️ 架构全景图（<150行）
├── ROADMAP.md                       # 📅 里程碑级进度（<100行，不拆任务）
│
├── gdd/                             # 📖 游戏设计文档（按系统拆分）
│   ├── index.md                     #   导航索引
│   ├── core-loop.md                 #   核心循环
│   ├── characters.md                #   角色系统
│   ├── weapons.md                   #   武器系统
│   ├── enemies.md                   #   敌人系统
│   ├── upgrades.md                  #   升级系统
│   ├── drops.md                     #   掉落系统
│   └── meta-progression.md          #   局外成长
│
├── tdd/                             # 🔧 技术设计文档（按系统拆分）
│   ├── index.md                     #   导航索引
│   ├── architecture.md              #   分层架构+管理器关系
│   ├── object-pool.md               #   对象池系统
│   ├── event-system.md              #   事件总线
│   ├── save-system.md               #   存档系统
│   └── ...                          #   按需新增
│
├── feature-specs/                   # 📋 功能规格书（Gameplay 系统的完整实现契约）
│   ├── index.md                     #   导航索引
│   ├── upgrade-system.md            #   升级系统
│   ├── weapon-system.md             #   武器系统
│   └── ...                          #   每个 Gameplay 功能一个文件
│
├── exec-plans/                      # 🚀 执行计划
│   ├── active/                      #   进行中的计划
│   ├── completed/                   #   已完成（归档，AI 通常不需要读）
│   └── tech-debt-tracker.md         #   技术债务追踪
│
├── references/                      # 📚 外部参考
│   └── ...
│
└── generated/                       # 🤖 自动生成（不要手动编辑）
    └── ...
```

---

## 3. 文件大小约束

| 文件 | 上限 | 超了怎么办 |
|------|------|-----------|
| AGENTS.md | 100 行 | 只保留导航和硬约束，详情移到对应文件 |
| CORE_BELIEFS.md | 50 行 | 只保留 5-7 条核心原则 |
| ARCHITECTURE.md | 150 行 | 只保留架构图和管理器关系，细节移到 tdd/ |
| gdd/*.md | 200 行 | 一个系统超 200 行，拆分子系统 |
| tdd/*.md | 200 行 | 同上 |
| feature-specs/*.md | 300 行 | 可稍长，AI 实现时需要完整契约 |
| exec-plans/active/*.md | 150 行 | 超过说明任务太大，拆分为多个 exec-plan |
| ROADMAP.md | 100 行 | 只有里程碑，不拆任务 |

---

## 4. 三层文档的关系与去重规则

### 4.1 职责划分

```
GDD（设计意图）          TDD（技术方案）         Feature-Spec（实现契约）
"武器自动攻击最近敌人"  → "WeaponBase抽象基类"  → "FindNearestEnemy()调用逻辑"
"最多6个武器槽"         → "PlayerWeapons.List"  → "槽位满时升级选项过滤规则"
"升级3选1"              → "UpgradeManager单例"   → "RollOptions(3)的完整交互流程"

    读者：策划/美术         读者：主程/架构师         读者：实现者（人或AI）
    问题：做什么？为什么？    问题：怎么组织？什么结构？  问题：怎么实现？边界在哪？
```

### 4.2 去重规则

- 同一条信息**只在一个层级写完整**，其他层级只引用
- GDD 写了"升级经验公式：5+3×L²" → TDD 不重复，只写"经验计算见 gdd/upgrades.md"
- TDD 写了"ObjectPool\<T\>.Get()" 接口 → feature-spec 不重复，只写"使用 PoolManager.Get()"
- feature-spec 写了完整的交互流程 → GDD 只写设计意图，不写流程细节

### 4.3 哪些系统需要 feature-spec？

| 系统类型 | tdd/ | feature-specs/ | 判断依据 |
|----------|------|----------------|----------|
| 基础设施（对象池、事件总线、存档） | ✅ 必须有 | ❌ 不需要 | 没有用户交互流程 |
| Gameplay系统（升级、武器、敌人生成） | ✅ 必须有 | ✅ 必须有 | 有交互流程、状态机、边界条件 |

**判断标准**：这个系统有"用户经历什么流程"吗？有 → 写 feature-spec；没有 → TDD 就够了。

---

## 5. AI 读取路径

### 5.1 按任务类型决定读什么

**任何单次任务，AI 读取的文档总量不应超过 context 的 25%。**

#### 新增一个武器

```
1. AGENTS.md                            → 硬约束（命名规范、对象池等）
2. gdd/weapons.md                       → 武器设计（类型、数值、升级树）
3. tdd/object-pool.md                   → 投射物必须走对象池
4. feature-specs/weapon-system.md       → 实现契约（接口、数据结构、流程）
❌ 不需要读 CORE_BELIEFS、ROADMAP、其他 gdd/
```

#### 修改升级选项权重

```
1. AGENTS.md                            → 数值不硬编码的约束
2. gdd/upgrades.md                      → 当前权重设计和设计意图
3. feature-specs/upgrade-system.md      → RollOptions 的实现逻辑
❌ 不需要读 tdd/、ROADMAP、其他 gdd/
```

#### 修 BUG（升级时卡顿）

```
1. AGENTS.md                            → 性能约束
2. feature-specs/upgrade-system.md      → 升级流程，定位卡顿位置
3. tdd/event-system.md                  → 卡顿与事件有关时
4. exec-plans/tech-debt-tracker.md      → 看是否已有相关技术债务
```

#### 新增一个系统（成就系统）

```
1. AGENTS.md                            → 硬约束
2. CORE_BELIEFS.md                      → 确认不违反核心原则
3. ARCHITECTURE.md                      → 新系统在架构中的位置
4. gdd/index.md                         → 确认成就系统不在已有 GDD 中
5. → 先写 gdd/achievements.md → 再写 tdd/achievements.md → 再写 feature-specs/achievements.md
```

### 5.2 Context 预算参考

假设 AI 可用 context 约 128K tokens（约 10 万汉字）：

| 场景 | 读取文件数 | 预估 tokens | 说明 |
|------|-----------|-------------|------|
| 小修改（改数值） | 1-2 个 | <3K | gdd/ 对应系统 + ScriptableObject |
| 新增功能 | 3-4 个 | 5-10K | AGENTS.md + gdd/ + tdd/ + feature-specs/ |
| 修 BUG | 2-3 个 | 3-8K | feature-specs/ + tdd/ + tech-debt |
| 新增系统 | 4-5 个 | 10-15K | AGENTS.md + CORE_BELIEFS + ARCHITECTURE + gdd/ + tdd/ |
| 大重构 | 5-6 个 | 15-25K | 上述全部 + exec-plans/ |

---

## 6. 各文件模板与示例

### 6.1 AGENTS.md — AI 唯一入口

**规则**：纯导航 + 硬约束。不写任何详细信息。

```markdown
# AGENTS.md

## 项目
暗夜幸存者 | Tuanjie 1.8.4 | C# | Built-in RP

## 硬约束（违反=BUG）
1. 频繁创建/销毁的对象走对象池，禁运行时Instantiate/Destroy
2. 系统间通信走GameEvents，禁跨Manager直接引用
3. 策划数值用ScriptableObject，不硬编码
4. 命名：PascalCase类名、camelCase局部、_camelCase私有字段
5. 目录：Scripts/{Core,Player,Weapons,Enemies,Drops,UI,Data}

## 核心原则
见 [CORE_BELIEFS.md](CORE_BELIEFS.md)

## 文档导航
| 需求 | 文档 |
|------|------|
| 游戏设计（做什么） | [gdd/index.md](gdd/index.md) |
| 技术方案（怎么组织） | [tdd/index.md](tdd/index.md) |
| 实现契约（怎么做） | [feature-specs/index.md](feature-specs/index.md) |
| 架构全景 | [ARCHITECTURE.md](ARCHITECTURE.md) |
| 开发进度 | [ROADMAP.md](ROADMAP.md) |
| 当前任务 | [exec-plans/active/](exec-plans/active/) |
| 技术债务 | [exec-plans/tech-debt-tracker.md](exec-plans/tech-debt-tracker.md) |

## 常见任务路径
- 新增武器 → gdd/weapons.md → tdd/object-pool.md → feature-specs/weapon-system.md
- 新增敌人 → gdd/enemies.md → tdd/object-pool.md → feature-specs/enemy-spawn-system.md
- 修改数值 → gdd/对应系统.md → 找ScriptableObject
- 修BUG → feature-specs/对应系统.md → tdd/对应系统.md → tech-debt-tracker.md
```

### 6.2 CORE_BELIEFS.md — 项目宪法

**规则**：只写 5-7 条不可违反的原则。超过 50 行就太多了。

```markdown
# 核心设计信念

> 不可违反。新功能违反以下任一条，必须经全团队讨论。

1. **单局25-35分钟** — 不做无限模式，机制导致局时偏移>5分钟则必须调整
2. **操作极简，深度在策略** — 只需移动，攻击全自动，主动技能上限2个
3. **每次选择都有意义** — 无"必选"（>80%选取率则削弱）无"废选项"（<5%则加强）
4. **性能是功能** — 60FPS/500敌人，新功能降帧>10%视为BUG，对象池强制
5. **数值不硬编码** — 策划可调数值一律ScriptableObject
```

### 6.3 gdd/index.md — GDD 导航索引

**规则**：系统名 + 一句话概要 + 链接 + 最后更新日期。不重复文档内容。

```markdown
# GDD 索引

> AI：根据你要实现的系统，只读对应文件，不要全读。

| 系统 | 文件 | 一句话概要 | 最后更新 |
|------|------|-----------|----------|
| 核心循环 | [core-loop.md](core-loop.md) | 移动躲避→自动攻击→升级选择→变强→循环 | 2025-02-15 |
| 角色系统 | [characters.md](characters.md) | 5角色，3头身，基础HP100，30级成长 | 2025-02-10 |
| 武器系统 | [weapons.md](weapons.md) | 4类武器，6槽位，每把3级升级 | 2025-02-15 |
| 敌人系统 | [enemies.md](enemies.md) | 普通精英Boss，环形生成，难度曲线 | 2025-02-08 |
| 升级系统 | [upgrades.md](upgrades.md) | 3选1，加权随机，武器/被动/升级 | 2025-02-10 |
| 掉落系统 | [drops.md](drops.md) | 经验宝石+金币，磁铁拾取 | 2025-02-05 |
| 局外成长 | [meta-progression.md](meta-progression.md) | 金币持久化，永久升级商店，角色解锁 | 2025-02-20 |
```

### 6.4 gdd/*.md — 单系统 GDD 文件

**规则**：写设计意图（做什么+为什么），不写实现细节。每个文件 <200 行。

```markdown
# 武器系统

> 设计意图：玩家通过升级获得新武器，武器自动攻击，体验来自"选择"而非"操作"。

## 武器类型
| 类型 | 描述 | 代表武器 |
|------|------|----------|
| 投射型 | 向最近敌人发射弹丸 | 飞剑、飞刀、能量球 |
| 轨道型 | 围绕角色旋转 | 旋转盾、光环 |
| 范围型 | 区域内持续伤害 | 圣水、火焰圈 |
| 辅助型 | 不直接伤害 | 加速光环、经验磁铁 |

## 武器槽位
- 最多同时持有 6 把武器
- 武器槽满后升级选项不再出现新武器

## 升级树
每把武器 3 级（基础 → 强化 → 终极），升级时需在升级选项中选中该武器的升级。

## 关键数值（见 ScriptableObject）
- 基础伤害/冷却/范围/弹数：各武器不同，见 WeaponData SO
- 升级加成：每级 +30% 基础伤害
```

### 6.5 tdd/index.md — TDD 导航索引

```markdown
# TDD 索引

> AI：根据你要实现的技术领域，只读对应文件。

| 领域 | 文件 | 核心类 | 最后更新 |
|------|------|--------|----------|
| 架构总览 | [architecture.md](architecture.md) | 分层+管理器关系+设计模式 | 2025-02-15 |
| 对象池 | [object-pool.md](object-pool.md) | ObjectPool<T>, PoolManager | 2025-02-01 |
| 事件系统 | [event-system.md](event-system.md) | GameEvents, 订阅规范 | 2025-02-01 |
| 存档 | [save-system.md](save-system.md) | SaveSlotManager, 序列化 | 2025-02-20 |
```

### 6.6 tdd/*.md — 单系统 TDD 文件

**规则**：数据结构 + 接口签名 + 关键决策理由。不写方法体实现。每个文件 <200 行。

```markdown
# 对象池系统

## 设计决策
- 使用泛型对象池而非ECS：项目规模小，对象池足以满足500+同屏需求
- 池预预热（Prewarm）：场景加载时预创建，避免运行时卡顿

## 核心接口
```csharp
public class ObjectPool<T> where T : Component
{
    T Get();                    // 从池获取，池空则创建
    void Return(T obj);         // 归还池中
    void Prewarm(int count);    // 预创建
}
```

## 关键约束
- 所有频繁创建/销毁的对象必须走对象池
- 严禁运行时 Instantiate/Destroy 这些对象
- 池上限：敌人300、投射物200、掉落物500、特效100

## 性能指标
- Get/Return 操作 < 0.1ms
- Prewarm 300个敌人 < 3帧
```

### 6.7 feature-specs/index.md — 功能规格书索引

```markdown
# 功能规格书索引

> AI：实现新功能前，先在这里找到对应的FS。FS是实现契约，不是设计意图（设计见gdd/）。
> 只有 Gameplay 系统需要 FS。基础设施系统（对象池、事件总线等）只需 tdd/。

| 功能 | 文档 | 状态 | 最后更新 |
|------|------|------|----------|
| 升级系统 | [upgrade-system.md](upgrade-system.md) | ✅已实现 | 2025-02-10 |
| 武器系统 | [weapon-system.md](weapon-system.md) | ✅已实现 | 2025-02-15 |
| 敌人生成 | [enemy-spawn-system.md](enemy-spawn-system.md) | ✅已实现 | 2025-02-08 |
| 角色选择 | [character-select.md](character-select.md) | 🔧开发中 | 2025-03-01 |
```

### 6.8 feature-specs/*.md — 单系统功能规格书

**规则**：实现级契约——交互流程、状态定义、边界条件、异常处理。每个文件 <300 行。

```markdown
# 升级系统 - 功能规格书

## 目标
玩家累积经验后暂停游戏，从3个随机选项中选择强化。

## 交互流程
1. 经验值 >= 升级所需 → 触发升级
2. 游戏暂停（Time.timeScale = 0）
3. 随机从候选池中抽取3个不重复选项
4. 显示升级UI：3张卡片，每张显示名称+描述+图标
5. 玩家点击选择 → 应用效果 → 关闭UI → 游戏恢复

## 选项类型
| 类型 | 来源 | 示例 |
|------|------|------|
| 新武器 | 武器池（尚未拥有的） | "获得飞剑" |
| 武器升级 | 已拥有武器的升级路径 | "飞剑 → 双重飞剑" |
| 被动道具 | 被动池 | "移速+10%" |

## 选项权重
- 新武器：权重 30（已有武器越少权重越高）
- 武器升级：权重 40
- 被动道具：权重 30
- 幸运值每点增加高品质选项权重 +5%

## 边界条件
- 所有武器已获得 → 新武器选项不再出现，权重重分配
- 武器已满级 → 该武器升级不再出现
- 武器槽位已满（6个）→ 新武器选项不再出现
- 升级期间受伤 → 不计算（游戏已暂停）

## 数据结构
- UpgradePool：所有可用选项的ScriptableObject列表
- UpgradeOption：{ 类型, 关联数据, 权重, 前置条件, 最大持有数 }
- PlayerUpgrades：当前已选升级的运行时记录

## 异常处理
- 候选池为空 → 显示"全满"，直接恢复游戏
- 3个选项不够（候选池<3）→ 有几个显示几个
```

### 6.9 exec-plans/active/*.md — 执行计划

**规则**：Step 级拆解 + 验收标准 + 决策记录。每个文件 <150 行。

```markdown
# 执行计划：新增Boss敌人

## 目标
第3关10分钟时生成Boss，击败后通关。

## 前置
- [x] 敌人系统（feature-specs/enemy-spawn-system.md）
- [x] 关卡系统

## 步骤
### Step 1：Boss数据
- 创建 BossData.cs（继承EnemyData，增加阶段列表）
- 创建 Boss_DarkKnight.asset
- ✅ 验收：SO可编辑

### Step 2：Boss行为
- 创建 BossBase.cs（继承EnemyBase）
- Phase1(追踪+范围攻击) → HP<50% → Phase2(召唤+冲刺)
- 🔧 开发中

### Step 3：Boss生成触发
- TimeManager 10分钟触发Boss事件
- EnemySpawner 停止普通敌人，生成Boss
- ⬜

### Step 4：Boss击败→胜利
- Boss死亡→GameEvents.BossDefeated→胜利界面
- ⬜

## 决策
| 日期 | 决策 | 理由 |
|------|------|------|
| 2025-03-10 | 2阶段而非3阶段 | 30分钟局中3阶段占比过长 |
```

### 6.10 exec-plans/tech-debt-tracker.md — 技术债务追踪

```markdown
# 技术债务追踪

## 活跃债务
| ID | 描述 | 影响 | 优先级 | 计划偿还 |
|----|------|------|--------|---------|
| TD-001 | OnHit每帧遍历debuff | >300敌人卡顿 | 高 | M4-W1 |
| TD-002 | 升级UI硬编码3卡槽 | 不支持4选1 | 低 | DLC1 |
| TD-003 | 存档JSON无版本号 | 无法迁移 | 中 | M4-W2 |

## 已偿还
| ID | 描述 | 日期 | 方式 |
|----|------|------|------|
| TD-005 | 投射物用Instantiate | 2025-02-01 | 改用PoolManager |
```

---

## 7. 文档健康检查清单

可脚本化，建议定期执行或 CI 化：

- [ ] AGENTS.md 行数 < 100
- [ ] CORE_BELIEFS.md 行数 < 50
- [ ] gdd/ tdd/ feature-specs/ 每个文件 < 300 行
- [ ] 所有 index.md 中的链接可访问
- [ ] AGENTS.md 中的链接可访问
- [ ] 无 `[待归档]` 标记超过 30 天的文档
- [ ] exec-plans/active/ 中无超过 2 周未更新的计划
- [ ] tech-debt-tracker.md 中无逾期高优先级债务
- [ ] ROADMAP.md 中只有里程碑，无任务拆解

---

## 8. Doc Gardening 流程

定期（建议每周）由人或 AI 智能体执行：

1. 扫描所有文档，检查交叉链接是否有效
2. 标记超过 1 个月未更新的活跃文档（可能已过时）
3. 检查"已完成"功能的 FS 是否与代码实际行为一致
4. 检查 gdd/tdd/feature-specs 是否有内容重复（违反去重规则）
5. 清理已废弃文档或添加重定向

---

## 9. 新增系统的文档创建流程

当需要新增一个系统时，按以下顺序创建文档：

```
1. gdd/<system>.md          — 先定义"做什么+为什么"
2. tdd/<system>.md          — 再定义"怎么组织+什么结构"
3. feature-specs/<system>.md — 最后定义"怎么实现+边界在哪"（仅 Gameplay 系统）
4. 更新 gdd/index.md        — 添加索引条目
5. 更新 tdd/index.md        — 添加索引条目
6. 更新 feature-specs/index.md — 添加索引条目（如有 FS）
```

创建前先检查 index.md，确认该系统不存在已有文档，避免重复。

---

*本文档版本：v1.1 | 创建日期：2025-05-16 | 最后更新：2026-06-08 (添加当前状态说明) | 独立于 DOCUMENTATION_GUIDE.md 使用*
