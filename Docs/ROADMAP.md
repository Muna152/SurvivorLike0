# 开发路线图 (Development Roadmap)

## 项目：暗夜幸存者 (Night Survivors)

## 项目信息

| 项目 | 值 |
|------|-----|
| **项目名称** | SurvivorLike0 |
| **引擎** | Tuanjie 1.8.4 (基于 Unity 2022.3.62t6) |
| **渲染管线** | Built-in Render Pipeline |
| **版本控制** | Git (本地仓库) |
| **项目路径** | `C:\Users\xiaochen.liu\SurvivorLike0` |
| **主要场景** | `Assets/Scenes/GameLevel.unity` |

---

## 阶段概览

```
阶段1: 基础框架 (MVP)     ████████████████  ~2周  ✅ 已完成
阶段2: 核心玩法完善          ████████████████  ~3周  ✅ 已完成
阶段3: 内容扩充              ████████████████  ~3周  ✅ 已完成
阶段4: 元进度与打磨          ██░░░░░░░░░░░░░  ~2周
─────────────────────────────────────────────
总计约 10 周
```

---

## 阶段1：基础框架 (MVP)

**目标：验证核心玩法循环，实现最小可玩原型**

### 1.1 项目搭建

- [x] 创建项目目录结构（Scripts/Core, Player, Weapons, Enemies, Drops, UI, Data）
- [x] 创建核心管理器框架（GameManager, PoolManager, EventManager）
- [x] 实现泛型对象池系统 `ObjectPool<T>`
- [x] 实现事件总线 `GameEvents`
- [x] 实现单例基类 `Singleton<T>`
- [x] 创建基础 ScriptableObject 数据类（WeaponData, EnemyData）

### 1.2 玩家系统

- [x] 实现 `PlayerController`（WASD/方向键移动）
- [x] 实现 `PlayerStats`（HP、移速、等级、经验值）
- [x] 创建玩家 Prefab（Sprite + Rigidbody2D + Collider）
- [x] 正交相机跟随玩家

### 1.3 武器系统（1把武器）

- [x] 实现 `WeaponBase` 抽象基类
- [x] 实现 `ProjectileWeapon`（飞剑）
- [x] 实现 `Projectile` 投射物（飞向最近敌人）
- [x] 实现自动攻击逻辑（Timer + FindNearestEnemy）
- [x] 创建飞剑 WeaponData ScriptableObject
- [x] 创建飞剑投射物 Prefab

### 1.4 敌人系统

- [x] 实现 `EnemyBase`（追踪玩家、受击、死亡）
- [x] 实现 `EnemySpawner`（环形区域生成）
- [x] 实现 `EnemyManager`（计数、管理活跃敌人）
- [x] 创建骷髅兵 EnemyData ScriptableObject
- [x] 创建骷髅兵 Prefab

### 1.5 掉落物系统

- [x] 实现 `DropBase`（经验宝石）
- [x] 实现 `DropManager`
- [x] 磁铁吸引逻辑（拾取范围内加速飞向玩家）
- [x] 创建经验宝石 Prefab

### 1.6 升级系统（简化版）

- [x] 实现 `UpgradeManager`（升级时生成3个选项）
- [x] 选项类型：新武器（飞剑）、武器升级、移速提升
- [x] 升级时暂停游戏
- [x] 实现基础 `UpgradeUI`（3选1卡片）

### 1.7 HUD

- [x] HP条（左上角，含背景条）
- [x] 经验条 + 等级显示 + EXP文字（当前/升级所需）
- [x] 计时器
- [x] 武器图标栏

### 1.8 游戏流程

- [x] 游戏开始 → 自动生成敌人
- [x] HP归零 → 游戏结束界面
- [x] 存活30分钟 → 胜利界面

**交付标准：可玩的核心循环 —— 移动 → 自动攻击 → 升级选择 → 死亡/存活**

---

## 阶段2：核心玩法完善

**目标：丰富武器和敌人种类，实现完整的升级/进化体系**

### 2.1 更多武器

- [x] 实现 `OrbitalWeapon`（旋转盾）
- [x] 实现 `AreaWeapon`（圣水、圣光）
- [x] 实现 `AuxiliaryWeapon`（护盾）
- [x] 飞刀武器（多弹数投射物）
- [x] 能量球武器（爆炸范围投射物）
- [x] 为每种武器创建8级 WeaponData
- [x] 为每种武器创建 Prefab 和投射物 Prefab

### 2.2 被动道具系统

- [x] 实现 `PassiveData` ScriptableObject (含 StatType 枚举、effectPerLevel、affectedStat、maxLevel)
- [x] 实现 `PassiveEffect` 系统（修改 PlayerStats 属性）
- [x] 实现8种被动道具的数据和效果（已有 PowerUp、SpeedBoost）
- [x] PlayerStats 被动道具管理（HasPassive/GetPassiveLevel/ApplyPassive）
- [x] 道具在升级选项中出现
- [x] 所有被动道具图标已生成并分配

### 2.3 武器进化

- [x] 实现6组武器进化路线
- [x] 宝箱掉落触发进化
- [x] 进化动画/特效
- [x] 进化武器数据和 Prefab
- [x] 进化武器不在普通升级池中出现（isEvolutionOnly 标记）

### 2.4 更多敌人

- [x] 蝙蝠（快速低HP）
- [x] 僵尸（高HP慢速）
- [x] 法师（远程攻击 + 投射物 MageProjectile）
- [x] 幽灵（穿越障碍）
- [x] 石像鬼（高HP冲锋）
- [x] 精英敌人系统（放大+变色+5倍HP）

### 2.5 空间分区优化

- [x] 实现 `SpatialGrid` 空间分区
- [x] 武器瞄准使用空间分区查询
- [x] 敌人移动时更新网格位置

### 2.6 难度曲线

- [x] 时间驱动的难度缩放系统
- [x] 敌人HP随时间增长
- [x] 生成密度和间隔随时间变化
- [x] 精英波次定时出现（数量随波次递增）

---

## 阶段3：内容扩充

**目标：多角色、BOSS、完整UI、关卡地图**

### 3.1 多角色

- [x] 实现 `CharacterData` ScriptableObject
- [x] 5个角色数据定义（起始武器、特殊被动、属性差异）
- [x] 角色选择界面
- [x] 角色解锁条件系统

### 3.2 BOSS系统

- [x] 实现 `BossEnemy`（继承 EnemyBase，增加BOSS行为）
- [x] 骷髅王（10min，召唤+震击）
- [x] 暗夜领主（20min，弹幕+传送+召唤精英）
- [x] 死神（30min，不可击杀）
- [x] BOSS血条UI

### 3.3 地图

- [x] 地面贴图/瓦片地图
- [x] 障碍物（树木、石头、墙壁）
- [x] 边界围栏
- [x] 环境装饰

### 3.4 完整UI

- [x] 主菜单界面（开始游戏/退出游戏 + 存档管理面板）
- [x] 角色选择界面
- [x] 暂停菜单
- [x] 完整结算界面（统计、金币、解锁提示）
- [x] 武器/角色图鉴界面

### 3.5 更多掉落物

- [x] 烤鸡（恢复HP）
- [x] 宝箱（触发进化）
- [x] 金币（局外货币）
- [x] 磁铁道具（临时增加拾取范围）
- [x] 经验宝石分级

---

## 阶段4：元进度与打磨

**目标：存档、永久成长、音效特效、数值平衡、性能优化**

### 4.1 存档与元进度

- [x] 存档栏位管理（SaveSlotManager, 3栏位, PlayerPrefs）
- [x] 角色解锁按存档隔离
- [x] 金币系统（GoldManager, 局内收集→局外持久化, PlayerPrefs）
- [x] 永久升级商店（UpgradeShopUI, 5种升级, 费用递增, GoldManager.PurchaseUpgrade）
- [x] 统计数据追踪（StatsTracker, 总击杀/游戏次数/最长存活/总金币, PlayerPrefs）

### 4.2 音效与特效

- [ ] BGM（战斗、菜单、BOSS战）
- [ ] 武器音效
- [ ] 敌人死亡音效
- [ ] 升级音效
- [ ] 受击特效
- [ ] 武器攻击特效
- [ ] 敌人死亡特效
- [ ] 经验拾取特效

### 4.3 数值平衡

- [ ] 调整各武器伤害/冷却/升级曲线
- [ ] 调整敌人HP/生成频率
- [ ] 调整经验值需求曲线
- [ ] 调整进化武器强度
- [ ] 调整永久升级费用
- [ ] 多轮测试迭代

### 4.4 性能优化

- [ ] 对象池全量覆盖（确认无运行时Instantiate/Destroy）
- [ ] 敌人LOD系统（远处低频更新）
- [ ] 碰撞Layer Matrix优化
- [ ] Sprite合批设置
- [ ] 内存分配审计（消除GC Spikes）
- [ ] Profiler性能分析与优化
- [ ] 目标：500敌人在60FPS稳定运行

### 4.5 Bug修复与打磨

- [ ] 边界情况处理
- [ ] UI适配和交互优化
- [ ] 动画和过渡优化
- [ ] 教程/提示系统

---

## 里程碑检查点

| 里程碑 | 验收标准 |
|--------|---------|
| **M1 - 可玩原型** | 阶段1完成，能移动→攻击→升级→死亡，核心循环可玩 |
| **M2 - 玩法完整** | 阶段2完成，6武器+进化+6敌人+被动道具，有深度 |
| **M3 - 内容充实** | 阶段3完成，5角色+3BOSS+地图+完整UI |
| **M4 - 可发布** | 阶段4完成，存档+音效+优化+平衡，达到可发布状态 |

---

*文档版本: v1.4 | 最后更新: 2026-05-08*
