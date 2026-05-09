# 实现进度追踪文档 (Implementation Tasks)

## 项目：暗夜幸存者 (Night Survivors)

> 本文档将 ROADMAP 中的开发任务分解为小型、可验收、适合 AI Agent 执行的原子任务。
> 每个任务应在一个 Agent 会话中可完成，产出明确的可验证产物。

## 项目信息

| 项目 | 值 |
|------|-----|
| **项目名称** | SurvivorLike0 |
| **引擎** | Tuanjie 1.8.4 (基于 Unity 2022.3.62t6) |
| **渲染管线** | Built-in Render Pipeline |
| **版本控制** | Git (本地仓库) |
| **项目路径** | `C:\Users\xiaochen.liu\SurvivorLike0` |
| **主要场景** | `Assets/Scenes/GameLevel.unity` |
| **状态标记** | ✅ 已完成 / ⬜ 待做 / 🔧 进行中 / ❌ 阻塞 |

---

## Phase 1: MVP 核心循环 (Week 1-3)

### T1.1 项目搭建

#### T1.1.1 创建项目目录结构
- **状态**: ✅
- **产物**: 完整 Assets/ 目录树
- **验收**: 所有必要文件夹存在

#### T1.1.2 实现 Singleton 单例基类
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/Singleton.cs`
- **验收**: 编译通过；泛型 Instance 属性可用

#### T1.1.3 实现 GameEvents 事件总线
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/GameEvents.cs`
- **验收**: 编译通过；所有事件可订阅和触发

#### T1.1.4 实现 ObjectPool 泛型对象池
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/ObjectPool.cs`
- **验收**: 编译通过；Get/Return/Prewarm 可用

#### T1.1.5 实现 PoolManager 池管理器
- **状态**: ✅
- **依赖**: T1.1.2, T1.1.4
- **产物**: `Scripts/Core/PoolManager.cs`
- **验收**: 编译通过；Register/Get/Return 可用

#### T1.1.6 实现 GameManager 游戏管理器骨架
- **状态**: ✅
- **依赖**: T1.1.2
- **产物**: `Scripts/Core/GameManager.cs`
- **验收**: 编译通过；GameState 枚举和生命周期方法可用

#### T1.1.7 实现 WeightedRandom 加权随机工具
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/WeightedRandom.cs`
- **验收**: 编译通过；Select/SelectN 可用

#### T1.1.8 创建 ScriptableObject 数据基类
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Data/WeaponData.cs`, `EnemyData.cs`, `CharacterData.cs`, `PassiveData.cs`, `LevelData.cs`
- **验收**: 编译通过；所有 SO 可在 Inspector 中创建

### T1.A 美术资产生成

#### T1.A.1 生成勇者角色 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Characters/Hero.png`
- **验收**: Sprite 资产存在

#### T1.A.2 生成骷髅兵敌人 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Enemies/Skeleton.png`
- **验收**: Sprite 资产存在

#### T1.A.3 生成飞剑投射物 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Weapons/FlyingSword.png`
- **验收**: Sprite 资产存在

#### T1.A.4 生成经验宝石和金币 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Drops/ExpGem.png`, `GoldCoin.png`
- **验收**: 两个 Sprite 资产存在

#### T1.A.5 生成地面草地 Material
- **状态**: ✅
- **产物**: `Art/Materials/GroundGrass.mat`
- **验收**: Material 资产存在

#### T1.A.6 生成 HUD/UI 基础 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/UI/HpBarFill.png`, `ExpBarFill.png`, `WeaponSlotFrame.png`
- **验收**: 3个 Sprite 资产存在

### T1.2 玩家系统

#### T1.2.1 实现 PlayerStats 属性管理
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Player/PlayerStats.cs`
- **验收**: 编译通过；HP/EXP/Level 系统可用

#### T1.2.2 实现 PlayerController 移动控制
- **状态**: ✅
- **依赖**: T1.2.1
- **产物**: `Scripts/Player/PlayerController.cs`
- **验收**: 编译通过；WASD 移动可用

#### T1.2.3 实现 PlayerHitbox 受击检测
- **状态**: ✅
- **依赖**: T1.2.1
- **产物**: `Scripts/Player/PlayerHitbox.cs`
- **验收**: 编译通过；敌人接触时扣血，无敌帧机制

#### T1.2.4 实现 PlayerWeaponManager 武器装备管理
- **状态**: ✅
- **依赖**: T1.1.8, T1.2.1
- **产物**: `Scripts/Player/PlayerWeaponManager.cs`
- **验收**: 编译通过；装备/升级/查询武器可用

#### T1.2.5 创建玩家 Prefab
- **状态**: ✅
- **依赖**: T1.2.2, T1.2.3, T1.2.4, T1.A.1
- **产物**: `Prefabs/Player/Player.prefab`
- **验收**: Prefab 包含所有必要组件

#### T1.2.6 设置正交相机跟随
- **状态**: ✅
- **依赖**: T1.2.5
- **产物**: `Scripts/Player/CameraFollow.cs`
- **验收**: 编译通过；相机跟随玩家

### T1.3 武器系统

#### T1.3.1 实现 WeaponBase 武器抽象基类
- **状态**: ✅
- **依赖**: T1.1.8, T1.2.1
- **产物**: `Scripts/Weapons/WeaponBase.cs`
- **验收**: 编译通过；冷却和自动攻击机制

#### T1.3.2 实现 ProjectileWeapon 投射型武器
- **状态**: ✅
- **依赖**: T1.3.1
- **产物**: `Scripts/Weapons/ProjectileWeapon.cs`
- **验收**: 编译通过；自动向最近敌人发射

#### T1.3.3 实现 Projectile 投射物组件
- **状态**: ✅
- **依赖**: T1.1.5, T1.1.8
- **产物**: `Scripts/Weapons/Projectile.cs`
- **验收**: 编译通过；飞行、命中、穿透、回收

#### T1.3.4 创建飞剑 WeaponData 和投射物 Prefab
- **状态**: ✅
- **依赖**: T1.3.3, T1.A.3
- **产物**: `Data/Weapons/FlyingSword.asset`, `Prefabs/Projectiles/FlyingSwordProj.prefab`
- **验收**: SO 和 Prefab 存在

#### T1.3.5 装备飞剑并验证自动攻击
- **状态**: ✅
- **依赖**: T1.2.5, T1.3.2, T1.3.4
- **验收**: Player 默认装备飞剑

### T1.4 敌人系统

#### T1.4.1 实现 EnemyBase 敌人基类
- **状态**: ✅
- **依赖**: T1.1.3, T1.1.5, T1.1.8
- **产物**: `Scripts/Enemies/EnemyBase.cs`
- **验收**: 编译通过；追踪、受击、死亡

#### T1.4.2 实现 EnemySpawner 敌人生成器
- **状态**: ✅
- **依赖**: T1.4.1
- **产物**: `Scripts/Enemies/EnemySpawner.cs`
- **验收**: 编译通过；环形区域持续生成

#### T1.4.3 实现 EnemyManager 敌人管理器
- **状态**: ✅
- **依赖**: T1.1.2, T1.4.1
- **产物**: `Scripts/Enemies/EnemyManager.cs`
- **验收**: 编译通过；生成和移除计数正确

#### T1.4.4 创建骷髅兵 EnemyData 和 Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T1.A.2
- **产物**: `Data/Enemies/Skeleton.asset`, `Prefabs/Enemies/Skeleton.prefab`
- **验收**: SO 和 Prefab 存在

### T1.5 掉落物系统

#### T1.5.1 实现 DropBase 掉落物基类
- **状态**: ✅
- **依赖**: T1.1.3, T1.1.5
- **产物**: `Scripts/Drops/DropBase.cs`
- **验收**: 编译通过；吸引和自动拾取

#### T1.5.2 实现 DropManager 掉落物管理器
- **状态**: ✅
- **依赖**: T1.1.2, T1.5.1
- **产物**: `Scripts/Drops/DropManager.cs`
- **验收**: 编译通过；敌亡掉落

#### T1.5.3 创建经验宝石和金币 Prefab
- **状态**: ✅
- **依赖**: T1.5.1, T1.A.4
- **产物**: `Prefabs/Drops/ExpGem.prefab`, `GoldCoin.prefab`
- **验收**: Prefab 存在

### T1.6 升级系统

#### T1.6.1 实现 UpgradeOption 升级选项
- **状态**: ✅
- **依赖**: T1.1.8
- **产物**: `Scripts/Upgrades/UpgradeOption.cs`
- **验收**: 编译通过；各选项 Apply() 可用

#### T1.6.2 实现 UpgradeManager 升级管理器
- **状态**: ✅
- **依赖**: T1.1.2, T1.6.1, T1.2.4
- **产物**: `Scripts/Upgrades/UpgradeManager.cs`
- **验收**: 编译通过；升级暂停游戏，选择后恢复

#### T1.6.3 实现 UpgradeUI 升级选择界面
- **状态**: ✅
- **依赖**: T1.6.2
- **产物**: `Scripts/Upgrades/UpgradeUI.cs`, `UpgradeCard.cs`
- **验收**: 编译通过；3选1界面

### T1.7 HUD

#### T1.7.1 实现 HP 条和经验条
- **状态**: ✅
- **依赖**: T1.2.1
- **产物**: `Scripts/UI/HUDController.cs`
- **验收**: 编译通过；HP 和经验条实时反映

#### T1.7.2 实现计时器显示
- **状态**: ✅
- **依赖**: T1.1.6
- **验收**: 编译通过；计时器正确显示

#### T1.7.3 实现武器图标栏
- **状态**: ✅
- **依赖**: T1.2.4
- **验收**: 编译通过；装备武器后底部显示图标

### T1.8 游戏流程

#### T1.8.1 创建 GameLevel 场景并整合系统
- **状态**: ✅
- **依赖**: T1.1.6, T1.2.5, T1.4.2, T1.5.2
- **产物**: `Scenes/GameLevel.unity`
- **验收**: 场景包含所有管理器和系统

#### T1.8.2 实现游戏结束/胜利界面
- **状态**: ✅
- **依赖**: T1.1.6, T1.7.1
- **产物**: `Scripts/UI/ResultScreen.cs`
- **验收**: 编译通过；HP归零显示GameOver，30分钟显示Victory

#### T1.8.3 HUD EXP文字显示
- **状态**: ✅
- **依赖**: T1.7.1
- **产物**: 更新 `Scripts/UI/HUDController.cs`（新增 `_expText` 字段 + `UpdateExpText()` 方法）
- **验收**: 经验条旁显示 "当前EXP/升级所需EXP" 文字

#### T1.8.4 HP/EXP条背景条 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/UI/HpBarBg.png`, `ExpBarBg.png`
- **验收**: HP和经验条均有背景底条

#### T1.8.5 UpgradeUI 事件驱动修复
- **状态**: ✅
- **依赖**: T1.6.3
- **产物**: 更新 `Scripts/Upgrades/UpgradeUI.cs`
- **说明**: 修复时序问题 — 改为 CanvasGroup 控制显隐；选择/跳过后通过 OnUpgradeComplete 事件驱动 Hide()，而非选择后立即 Hide()
- **验收**: 升级选择后UI正确隐藏，无闪烁或过早关闭

#### T1.8.6 OrbitalWeapon Prefab回退修复
- **状态**: ✅
- **依赖**: T2.1.1
- **产物**: 更新 `Scripts/Weapons/OrbitalWeapon.cs`
- **说明**: 运行时创建轨道武器时 `_orbitalPrefab` 为空，新增从 `WeaponData.projectilePrefab` 自动获取的逻辑
- **验收**: 运行时装备旋转盾不再丢失轨道物体引用

#### T1.8.7 经验值公式改为二次曲线
- **状态**: ✅
- **依赖**: T1.2.1
- **产物**: 更新 `Scripts/Player/PlayerStats.cs`
- **说明**: 原公式 `10 + (level-1)*5`（线性）改为 `5 + 5*level²`（二次），前期升级快后期慢
- **验收**: 升级经验随等级二次增长

#### T1.8.8 进化武器不出现在普通升级池
- **状态**: ✅
- **依赖**: T2.4.1, T1.6.2
- **产物**: 更新 `Scripts/Data/WeaponData.cs`（新增 isEvolutionOnly 字段）, 更新 `Scripts/Upgrades/UpgradeManager.cs`（过滤进化武器）
- **说明**: 进化武器（Excalibur 等）仅在满足进化条件时通过 WeaponEvolutionOption 出现，不再作为 NewWeaponOption 刷出
- **验收**: 普通升级选项中不会出现进化武器

#### T1.8.9 升级卡片图标补全
- **状态**: ✅
- **依赖**: T1.6.3
- **产物**: 生成并分配12个武器图标 + 10个被动道具图标
- **说明**: 所有 WeaponData 和 PassiveData 的 icon 字段均已分配 Sprite
- **验收**: 升级选择界面所有卡片都有图标显示

#### T1.8.10 武器升级描述增强
- **状态**: ✅
- **依赖**: T1.6.1
- **产物**: 更新 `Scripts/Upgrades/UpgradeOption.cs`
- **说明**: 武器升级描述从显示绝对数值改为对比格式（DMG 10→12, +1 Projectile, +1 Pierce 等）；新武器描述增加类型标签；满级显示 ★ 特殊效果
- **验收**: 升级卡片清晰展示每项属性变化

---

## Phase 2: 核心玩法完善 (Week 4-6)

### T2.1 武器种类扩充

#### T2.1.1 实现 OrbitalWeapon 轨道型武器基类
- **状态**: ✅
- **依赖**: T1.3.1
- **产物**: `Scripts/Weapons/OrbitalWeapon.cs`
- **验收**: 编译通过；轨道物体围绕角色旋转攻击

#### T2.1.2 实现 OrbitalObject 轨道物体组件
- **状态**: ✅
- **依赖**: T1.1.5, T2.1.1
- **产物**: `Scripts/Weapons/OrbitalObject.cs`
- **验收**: 编译通过；旋转、碰撞敌人、可配置轨道半径/角速度/数量

#### T2.1.3 实现 AreaWeapon 范围型武器基类
- **状态**: ✅
- **依赖**: T1.3.1
- **产物**: `Scripts/Weapons/AreaWeapon.cs`
- **验收**: 编译通过；在指定区域内持续造成伤害/治疗

#### T2.1.4 实现 AuxiliaryWeapon 辅助型武器基类
- **状态**: ✅
- **依赖**: T1.3.1
- **产物**: `Scripts/Weapons/AuxiliaryWeapon.cs`
- **验收**: 编译通过；不直接伤害，提供增益效果

#### T2.1.5 创建旋转盾 WeaponData + Prefab
- **状态**: ✅
- **依赖**: T2.1.1, T2.1.2, T2.A.1
- **产物**: `Data/Weapons/SpinningShield.asset`, `Prefabs/Weapons/SpinningShield.prefab`
- **验收**: 8级数据完整；旋转盾围绕玩家旋转

#### T2.1.6 创建飞刀 WeaponData + 投射物 Prefab
- **状态**: ✅
- **依赖**: T1.3.2, T2.A.2
- **产物**: `Data/Weapons/Knife.asset`, `Prefabs/Projectiles/KnifeProj.prefab`
- **验收**: 8级数据完整；多弹数快速投射物

#### T2.1.7 创建能量球 WeaponData + 投射物 Prefab
- **状态**: ✅
- **依赖**: T1.3.2, T2.A.3
- **产物**: `Data/Weapons/EnergyBall.asset`, `Prefabs/Projectiles/EnergyBallProj.prefab`
- **验收**: 8级数据完整；命中后爆炸范围伤害

#### T2.1.8 创建圣光 WeaponData + Prefab
- **状态**: ✅
- **依赖**: T2.1.3, T2.A.4
- **产物**: `Data/Weapons/HolyLight.asset`, `Prefabs/Weapons/HolyLightZone.prefab`
- **验收**: 8级数据完整；持续治疗区域

#### T2.1.9 创建圣水 WeaponData + Prefab
- **状态**: ✅
- **依赖**: T2.1.3, T2.A.4
- **产物**: `Data/Weapons/HolyWater.asset`, `Prefabs/Weapons/HolyWaterZone.prefab`
- **验收**: 8级数据完整；地面持续伤害区域

#### T2.1.10 更新 PlayerWeaponManager 支持新武器类型
- **状态**: ✅
- **依赖**: T2.1.1, T2.1.3, T2.1.4, T1.2.4
- **产物**: 更新 `Scripts/Player/PlayerWeaponManager.cs`
- **验收**: 编译通过；可正确装备 Orbital/Area/Auxiliary 类型武器

#### T2.1.11 更新 UpgradeManager 将新武器加入升级池
- **状态**: ✅
- **依赖**: T2.1.5, T2.1.6, T2.1.7, T2.1.8, T2.1.9, T1.6.2
- **产物**: 更新 `Scripts/Upgrades/UpgradeManager.cs`
- **验收**: 升级选项中可出现旋转盾/飞刀/能量球/圣光/圣水

### T2.A 美术资产生成（Phase 2）

#### T2.A.1 生成旋转盾 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Weapons/SpinningShield.png`
- **验收**: Sprite 资产存在

#### T2.A.2 生成飞刀 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Weapons/Knife.png`
- **验收**: Sprite 资产存在

#### T2.A.3 生成能量球 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Weapons/EnergyBall.png`
- **验收**: Sprite 资产存在

#### T2.A.4 生成圣光和圣水 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Weapons/HolyLight.png`, `HolyWater.png`
- **验收**: 2个 Sprite 资产存在

#### T2.A.5 生成新敌人 Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/Enemies/Bat.png`, `Zombie.png`, `Mage.png`, `Ghost.png`, `Gargoyle.png`
- **验收**: 5个 Sprite 资产存在

#### T2.A.6 生成被动道具图标 Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/UI/Wing.png` (空翼), `Bracer.png` (护腕), `Magnet.png` (磁铁), `Codex.png` (法典), `Heart.png` (心), `Bone.png` (骨头), `Shell.png` (甲壳), `Feather.png` (翅膀)
- **验收**: 8个 Sprite 资产存在

#### T2.A.7 生成宝箱 Sprite
- **状态**: ✅
- **产物**: `Art/Sprites/Drops/Chest.png`
- **验收**: Sprite 资产存在

### T2.2 敌人种类扩充

#### T2.2.1 创建蝙蝠 EnemyData + Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T2.A.5
- **产物**: `Data/Enemies/Bat.asset`, `Prefabs/Enemies/Bat.prefab`
- **验收**: HP=5, 移速=3.0, 伤害=3, 快速低HP敌人

#### T2.2.2 创建僵尸 EnemyData + Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T2.A.5
- **产物**: `Data/Enemies/Zombie.asset`, `Prefabs/Enemies/Zombie.prefab`
- **验收**: HP=25, 移速=1.0, 伤害=8, 高HP慢速敌人

#### T2.2.3 创建法师 EnemyData + Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T2.2.4, T2.A.5
- **产物**: `Data/Enemies/Mage.asset`, `Prefabs/Enemies/Mage.prefab`
- **验收**: HP=15, 移速=1.2, 伤害=12, 远程投射攻击

#### T2.2.4 实现法师投射物组件
- **状态**: ✅
- **说明**: MageProjectile.cs + MageEnemy.cs 已实现；MageProj.prefab 已创建；法师 Prefab 已挂载 MageEnemy 组件并配置投射物引用
- **依赖**: T1.1.5
- **产物**: `Scripts/Enemies/MageProjectile.cs`, `Prefabs/Projectiles/MageProj.prefab`
- **验收**: 编译通过；法师发射投射物攻击玩家

#### T2.2.5 创建幽灵 EnemyData + Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T2.A.5
- **产物**: `Data/Enemies/Ghost.asset`, `Prefabs/Enemies/Ghost.prefab`
- **验收**: HP=8, 移速=2.5, 伤害=6, 可穿越障碍物

#### T2.2.6 创建石像鬼 EnemyData + Prefab
- **状态**: ✅
- **依赖**: T1.4.1, T2.A.5
- **产物**: `Data/Enemies/Gargoyle.asset`, `Prefabs/Enemies/Gargoyle.prefab`
- **验收**: HP=50, 移速=1.8, 伤害=15, 高HP冲锋敌人

#### T2.2.7 实现精英敌人系统
- **状态**: ✅
- **依赖**: T1.4.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`, 新增 `Scripts/Enemies/EliteEnemy.cs`
- **验收**: 精英敌人放大1.5倍+变色+5倍HP+2倍伤害；死亡必掉大经验宝石

#### T2.2.8 更新 EnemySpawner 支持多种敌人生成
- **状态**: ✅
- **依赖**: T2.2.1, T2.2.2, T2.2.3, T2.2.5, T2.2.6, T1.4.2
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`
- **验收**: 生成器根据时间和权重选择不同敌人类型

### T2.3 被动道具系统完善

#### T2.3.1 完善 PassiveData ScriptableObject
- **状态**: ✅
- **依赖**: T1.1.8
- **产物**: 更新 `Scripts/Data/PassiveData.cs`
- **验收**: 包含 effectPerLevel, affectedStat (StatType枚举), maxLevel, icon, description, requiredForEvolutionWeaponId

#### T2.3.2 实现 PassiveEffect 系统
- **状态**: ✅
- **依赖**: T2.3.1, T1.2.1
- **产物**: `Scripts/Upgrades/PassiveEffect.cs`
- **验收**: 编译通过；ApplyEffect 可修改 PlayerStats 对应属性

#### T2.3.3 创建8种被动道具数据
- **状态**: ✅
- **依赖**: T2.3.1, T2.A.6
- **产物**: `Data/Passives/Wing.asset` (空翼:移速+8%), `Bracer.asset` (护腕:伤害+10%), `Magnet.asset` (磁铁:拾取+25%), `Codex.asset` (法典:冷却-8%), `Heart.asset` (心:HP+10%), `Bone.asset` (骨头:幸运+10), `Shell.asset` (甲壳:护甲+1), `Feather.asset` (翅膀:弹数+1/2级)
- **验收**: 8个 SO 资产存在，各5级数据完整

#### T2.3.4 实现 PlayerStats 被动道具管理
- **状态**: ✅
- **依赖**: T2.3.2, T1.2.1
- **产物**: 更新 `Scripts/Player/PlayerStats.cs`
- **验收**: HasPassive(), GetPassiveLevel(), ApplyPassive() 可用；属性实时更新

#### T2.3.5 更新 UpgradeManager 将被动道具加入升级选项
- **状态**: ✅
- **依赖**: T2.3.3, T2.3.4, T1.6.2
- **产物**: 更新 `Scripts/Upgrades/UpgradeManager.cs`, `Scripts/Upgrades/UpgradeOption.cs`
- **验收**: 升级选项中可出现被动道具；已持有道具可升级

### T2.4 武器进化系统

#### T2.4.1 定义6组进化路线数据
- **状态**: ✅
- **依赖**: T1.1.8, T2.3.1
- **产物**: 更新 `Scripts/Data/WeaponData.cs`（进化字段 + isEvolutionOnly 标记）+ 更新6个现有 WeaponData 的进化关联
- **验收**: 每把武器可配置 canEvolve, requiredPassive, evolvedWeapon; 进化武器标记 isEvolutionOnly 不出现在普通升级池

#### T2.4.2 实现宝箱掉落触发进化逻辑
- **状态**: ✅
- **依赖**: T2.4.1, T1.5.1, T1.2.4
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（宝箱收集逻辑）, 更新 `Scripts/Player/PlayerWeaponManager.cs`（进化方法）
- **验收**: 拾取宝箱时检查进化条件；满足则替换为进化武器

#### T2.4.3 创建6把进化武器数据
- **状态**: ✅
- **依赖**: T2.4.1
- **产物**: `Data/Weapons/Excalibur.asset` (圣剑), `InfiniteKnife.asset` (无限飞刀), `JudgementWheel.asset` (审判之轮), `VoidBlackHole.asset` (虚空黑洞), `AngelsSong.asset` (天使之歌), `UndeadFlood.asset` (亡灵洪流)
- **验收**: 6个进化武器 SO 存在，包含完整等级数据，isEvolutionOnly=true

#### T2.4.4 创建进化武器 Prefab 和投射物
- **状态**: ✅
- **依赖**: T2.4.3
- **产物**: 对应进化武器的 Prefab 和投射物 Prefab
- **验收**: 所有进化武器 Prefab 可正确实例化和攻击

#### T2.4.5 实现进化动画和特效
- **状态**: ✅
- **依赖**: T2.4.2
- **产物**: `Scripts/Weapons/WeaponEvolutionVFX.cs`
- **验收**: 武器进化时播放闪光/变身特效

### T2.5 空间分区优化

#### T2.5.1 实现 SpatialGrid 空间分区网格
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/SpatialGrid.cs`
- **验收**: 编译通过；Insert/Remove/Update/FindNearest/FindInRange/FindNearestN 可用

#### T2.5.2 武器瞄准使用空间分区查询
- **状态**: ✅
- **依赖**: T2.5.1, T1.3.2
- **产物**: 更新 `Scripts/Weapons/ProjectileWeapon.cs`（替代 FindObjectsOfType）
- **验收**: 武器通过 SpatialGrid.FindNearest 查找目标

#### T2.5.3 敌人移动时更新网格位置
- **状态**: ✅
- **依赖**: T2.5.1, T1.4.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`
- **验收**: 敌人每帧更新位置到 SpatialGrid

#### T2.5.4 性能验证
- **状态**: ✅
- **依赖**: T2.5.2, T2.5.3
- **验收**: 500敌人场景下空间分区查询性能优于 FindObjectsOfType

### T2.6 难度曲线与角色解锁

#### T2.6.1 实现时间驱动的难度缩放系统
- **状态**: ✅
- **依赖**: T1.1.6
- **产物**: `Scripts/Core/DifficultyManager.cs`
- **验收**: 编译通过；提供当前难度系数、敌人HP倍率、生成间隔等查询

#### T2.6.2 敌人HP随时间增长
- **状态**: ✅
- **依赖**: T2.6.1, T1.4.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`
- **验收**: 敌人HP按 `baseHP × (1 + 0.1 × minutes)` 缩放

#### T2.6.3 生成密度和间隔随时间变化
- **状态**: ✅
- **依赖**: T2.6.1, T1.4.2
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`
- **验收**: 生成间隔按 `baseInterval / (1 + 0.15 × minutes)` 递减

#### T2.6.4 精英波次定时出现
- **状态**: ✅
- **依赖**: T2.6.1, T2.2.7
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`（精英波次逻辑）
- **验收**: 每5分钟出现一波精英敌人

#### T2.6.5 实现角色解锁条件系统
- **状态**: ✅
- **依赖**: T1.2.1
- **产物**: `Scripts/Data/UnlockCondition.cs`, 更新 `Scripts/Data/CharacterData.cs`
- **验收**: 角色可配置解锁条件（存活时间/击杀数/治疗量）；游戏内可查询解锁状态

---

## Phase 3: 内容扩充 (Week 7-8)

### T3.1 多角色系统

#### T3.1.1 完善 CharacterData ScriptableObject
- **状态**: ✅
- **依赖**: T1.1.8
- **产物**: 更新 `Scripts/Data/CharacterData.cs`
- **说明**: 新增 id(string), description(TextArea), isDefaultUnlocked(bool) 字段；移除 unlocked 字段，改由 UnlockManager 运行时管理
- **验收**: 包含 id, description, isDefaultUnlocked, baseHP, moveSpeed, armor, regen, projectileBonus, cooldownMultiplier, startingWeapon, portrait, unlockCondition

#### T3.1.2 创建5个角色数据定义
- **状态**: ✅
- **依赖**: T3.1.1
- **产物**: `Data/Characters/Hero.asset` (勇者:飞剑/HP120), `Mage.asset` (法师:能量球/HP70/冷却0.9), `Knight.asset` (骑士:旋转盾/HP100/护甲2), `Ranger.asset` (游侠:飞刀/HP80/弹数+1), `Priest.asset` (牧师:圣光/HP90/再生0.5)
- **验收**: 5个角色 SO 存在，各属性差异化，起始武器引用正确

#### T3.1.3 实现角色选择界面
- **状态**: ✅
- **依赖**: T3.1.2
- **产物**: `Scripts/UI/CharacterSelectUI.cs`
- **说明**: 编程式构建UI（HorizontalLayoutGroup），5张角色卡片含肖像/名称/属性/描述/锁定状态；详情面板；"开始战斗"按钮调用 GameManager.StartGame(character)
- **验收**: 显示5个角色卡片；未解锁角色灰色+解锁条件提示；选中后开始游戏

#### T3.1.4 实现角色解锁逻辑
- **状态**: ✅
- **依赖**: T2.6.5, T3.1.2
- **产物**: `Scripts/Core/UnlockManager.cs` (Singleton), 更新 `Scripts/Core/GameEvents.cs` (OnCharacterUnlocked事件)
- **说明**: PlayerPrefs持久化("Unlock_{id}")；CheckUnlocks()在游戏结算时调用；发现新解锁触发OnCharacterUnlocked事件
- **验收**: 游戏结算时检查解锁条件；达成条件弹出解锁提示

#### T3.1.5 生成其他4个角色 Sprite
- **状态**: ✅
- **产物**: `Sprites/Characters/MagePortrait.png`, `KnightPortrait.png`, `RangerPortrait.png`, `PriestPortrait.png`
- **验收**: 4个角色 Sprite 资产存在，Sprite导入设置正确

### T3.2 BOSS 系统

#### T3.2.1 实现 BossEnemy 基类
- **状态**: ✅
- **依赖**: T1.4.1
- **产物**: `Scripts/Enemies/BossEnemy.cs`
- **验收**: 编译通过；BOSS行为（多阶段、特殊攻击模式、血条显示）

#### T3.2.2 创建骷髅王 BOSS
- **状态**: ✅
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Bosses/SkeletonKing.asset`, `Prefabs/Bosses/SkeletonKing.prefab`, `Scripts/Enemies/SkeletonKing.cs`
- **验收**: 10分钟出现；HP=500；召唤骷髅群 + 范围震击攻击

#### T3.2.3 创建暗夜领主 BOSS
- **状态**: ✅
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Bosses/DarkLord.asset`, `Prefabs/Bosses/DarkLord.prefab`, `Scripts/Enemies/DarkLord.cs`
- **验收**: 20分钟出现；HP=2000；全屏弹幕 + 传送 + 召唤精英

#### T3.2.4 创建死神 BOSS
- **状态**: ✅
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Bosses/DeathBoss.asset`, `Prefabs/Bosses/DeathBoss.prefab`, `Scripts/Enemies/DeathBoss.cs`
- **验收**: 30分钟出现；HP=5000；追踪弹幕 + 扩散攻击；不可被击杀（存活到时间结束）

#### T3.2.5 实现 BOSS 血条 UI
- **状态**: ✅
- **依赖**: T3.2.1
- **产物**: `Scripts/UI/BossHealthBar.cs`
- **验收**: BOSS出现时屏幕顶部显示血条；HP实时更新

#### T3.2.6 更新 EnemySpawner 定时生成 BOSS
- **状态**: ✅
- **依赖**: T3.2.2, T3.2.3, T3.2.4, T1.4.2
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`, `Scripts/Enemies/BossProjectile.cs`, `Scripts/Enemies/BossShockwave.cs`
- **验收**: 10/20/30分钟自动生成对应 BOSS

### T3.A 美术资产生成（Phase 3）

#### T3.A.1 生成 BOSS Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/Enemies/SkeletonKing.png`, `DarkLord.png`, `Death.png`
- **验收**: 3个 BOSS Sprite 资产存在

#### T3.A.2 生成地图环境 Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/Environment/Tree.png`, `Rock.png`, `Wall.png`, `Fence.png`
- **验收**: 环境装饰 Sprite 资产存在

#### T3.A.3 生成新掉落物 Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/Drops/RoastChicken.png`, `Chest.png`, `MagnetItem.png`
- **验收**: 3个掉落物 Sprite 资产存在

#### T3.A.4 生成 UI 界面 Sprites
- **状态**: ✅
- **产物**: `Art/Sprites/UI/MenuBackground.png`, `CharacterCardFrame.png`, `BossBarFrame.png`
- **验收**: UI Sprite 资产存在（MenuBackground 透明背景，is_segmentation=true）

### T3.3 地图系统

#### T3.3.1 实现地面贴图/瓦片地图
- **状态**: ✅
- **依赖**: T1.A.5
- **产物**: 更新 GameLevel 场景地面
- **验收**: 使用 Tilemap 或大贴图铺设200×200地图

#### T3.3.2 实现障碍物（树木、石头、墙壁）
- **状态**: ✅
- **依赖**: T3.A.2
- **产物**: `Prefabs/Environment/Tree.prefab`, `Rock.prefab`, `Wall.prefab`
- **验收**: 障碍物有碰撞体；敌人无法穿过（幽灵除外）；投射物被阻挡

#### T3.3.3 实现边界围栏
- **状态**: ✅
- **依赖**: T1.2.5
- **产物**: 更新 GameLevel 场景边界
- **验收**: 玩家和敌人无法离开200×200地图区域

#### T3.3.4 环境装饰
- **状态**: ✅
- **依赖**: T3.A.2, T3.3.2
- **产物**: 地图中的装饰物件（草丛、花、碎石等）
- **验收**: 地图有视觉层次和丰富度

### T3.4 完整 UI

#### T3.4.1 实现主菜单/大厅界面
- **状态**: ✅
- **依赖**: T3.A.4
- **产物**: `Scripts/UI/MainMenuUI.cs`
- **说明**: 编程式构建主菜单UI（无Prefab），包含"开始游戏"/"退出游戏"按钮 + 左上角"存档管理"弹出面板。MainMenuUI 显示时隐藏 HUD 元素。需要先选择存档才能开始游戏。CharacterSelectUI 新增"返回主菜单"按钮，不再自动显示，由 MainMenuUI 控制显隐。
- **验收**: 主菜单正常显示；存档管理可创建/删除/切换3个存档；开始游戏进入角色选择

#### T3.4.2 实现角色选择界面
- **状态**: ✅
- **依赖**: T3.1.3, T3.4.1
- **产物**: 完善 `Scripts/UI/CharacterSelectUI.cs`
- **说明**: 已实现5角色卡片选择+锁定状态+详情面板；新增"返回主菜单"按钮；由 MainMenuUI 控制显隐
- **验收**: 显示5角色卡片，选中高亮，未解锁灰色+条件提示，可返回主菜单

#### T3.4.3 实现暂停菜单
- **状态**: ✅
- **依赖**: T1.1.6
- **产物**: `Scripts/UI/PauseMenuController.cs`
- **说明**: 已有完整ESC暂停/继续/重新开始/回到菜单功能，新增图鉴按钮

#### T3.4.4 完善结算界面
- **状态**: ✅
- **依赖**: T1.8.2
- **产物**: 更新 `Scripts/UI/ResultScreen.cs`
- **说明**: 显示存活时间/击杀数/等级/金币/精英击杀/总治疗/角色名/解锁提示；再来一局/回到菜单按钮

#### T3.4.5 实现武器/角色图鉴界面
- **状态**: ✅
- **依赖**: T2.3.3, T3.1.2
- **产物**: `Scripts/UI/CodexUI.cs`
- **说明**: 武器标签页(含进化路线) + 角色标签页(含解锁条件)；暂停菜单可打开；编程式构建

### T3.5 更多掉落物

#### T3.5.1 实现烤鸡掉落物
- **状态**: ✅
- **依赖**: T1.5.1, T3.A.3
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Health类型）, `Prefabs/Drops/RoastChicken.prefab`
- **验收**: 敌人5%几率掉落；拾取恢复30HP

#### T3.5.2 实现宝箱掉落物
- **状态**: ✅
- **依赖**: T2.4.2, T3.A.3
- **产物**: `Prefabs/Drops/Chest.prefab`
- **验收**: 精英/BOSS掉落；拾取触发武器进化（条件满足时）

#### T3.5.3 实现磁铁道具掉落
- **状态**: ✅
- **依赖**: T1.5.1, T3.A.3
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Magnet类型）, `Prefabs/Drops/MagnetItem.prefab`
- **验收**: 拾取后临时增加拾取范围10秒

#### T3.5.4 完善金币掉落物
- **状态**: ✅
- **依赖**: T1.5.1
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Gold收集逻辑）
- **验收**: 金币正确计入局外货币

#### T3.5.5 实现经验宝石分级
- **状态**: ✅
- **依赖**: T1.5.1
- **产物**: `Prefabs/Drops/ExpGemSmall.prefab` (+1), `ExpGemMedium.prefab` (+5), `ExpGemLarge.prefab` (+20)
- **验收**: 普通/精英/BOSS掉落不同等级经验宝石

---

## Phase 4: 元进度与打磨 (Week 9-10)

### T4.1 存档与元进度

#### T4.1.1 实现 SaveSlotManager 存档管理器
- **状态**: ✅
- **依赖**: T1.1.1
- **产物**: `Scripts/Data/SaveSlotManager.cs`, 更新 `Scripts/Data/UnlockManager.cs`
- **说明**: SaveSlotManager 为纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理3个存档栏位。每个存档有字符串名称，不允许重名。解锁数据通过 `Save_{slotIndex}_Unlock_{charId}` 前缀按存档隔离。切换存档时自动重载 UnlockManager。主菜单集成了存档管理面板。
- **验收**: 编译通过；3栏位创建/删除/切换可用；解锁状态按存档隔离；切换存档后角色解锁状态正确

#### T4.1.2 实现金币系统
- **状态**: ✅
- **依赖**: T4.1.1, T3.5.4
- **产物**: `Scripts/Core/GoldManager.cs`, 更新 `Scripts/Core/GameEvents.cs`, 更新 `Scripts/Player/PlayerStats.cs`, 更新 `Scripts/UI/ResultScreen.cs`, 更新 `Scripts/UI/HUDController.cs`
- **说明**: GoldManager 为纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理每存档金币余额。包含 PermanentUpgradeType 枚举（HPBonus/MoveSpeedBonus/DamageBonus/PickupRangeBonus/ExtraLife）、5种升级费用表（GDD 9.2）、PurchaseUpgrade()/ApplyPermanentUpgrades(PlayerStats) 方法。PlayerStats 新增 ExtraLives 属性，TakeDamage() 中额外生命在 OnPlayerDied 前拦截（复活回50%HP）。GameEvents 新增 OnGoldChanged/OnPermanentUpgradePurchased 事件。ResultScreen 调用 GoldManager.AddGold() 持久化局内金币（_persisted 防重入）。HUDController 新增金币文字 💰（change-detected 刷新，无每帧字符串分配）。
- **验收**: 局内金币收集→局外可查看余额；跨局持久化；HUD实时显示金币

#### T4.1.3 实现永久升级商店
- **状态**: ✅
- **依赖**: T4.1.2
- **产物**: `Scripts/UI/UpgradeShopUI.cs`, 更新 `Scripts/UI/MainMenuUI.cs`
- **说明**: UpgradeShopUI 为编程式UI（CanvasGroup驱动显隐），展示5个升级行（名称/描述/等级指示器■□□□□/费用/购买按钮）。购买通过 GoldManager.PurchaseUpgrade()，购买后自动刷新并触发 OnPermanentUpgradePurchased 事件。MainMenuUI 新增绿色"🛒 商店"按钮（仅激活存档时可交互），点击时创建/显示 UpgradeShopUI。
- **验收**: 大厅中可花费金币购买永久加成（HP/移速/伤害/拾取/额外生命）；费用递增

#### T4.1.4 实现解锁系统
- **状态**: ✅
- **依赖**: T4.1.1, T3.1.4
- **产物**: 已在 T3.1.4 / T4.1.1 中完成（UnlockManager + SaveSlotManager 集成）
- **说明**: 验证确认 UnlockManager 已完整实现存档隔离的解锁持久化，SaveSlotManager.DeleteSlot() 已调用 UnlockManager.ClearSlotData()、GoldManager.ClearSlotData()、StatsTracker.ClearSlotStats()。
- **验收**: 角色和武器解锁状态持久化；新游戏可使用已解锁角色；删除存档清理所有关联数据

#### T4.1.5 实现统计数据追踪
- **状态**: ✅
- **依赖**: T4.1.1
- **产物**: `Scripts/Core/StatsTracker.cs`, 更新 `Scripts/Data/SaveSlotManager.cs`
- **说明**: StatsTracker 为纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理每存档累计统计。追踪 TotalKills/TotalGames/BestSurvivalTime/TotalGoldEarned，键格式 `Save_{slot}_Stat_{statName}`。ResultScreen 在 Show() 时调用各更新方法（_persisted 防重入）。SaveSlotManager.DeleteSlot() 调用 ClearSlotStats(index) 清理。
- **验收**: 追踪总击杀数/总游戏次数/最长存活时间/总金币等；跨局持久化

### T4.A 音效资产生成（Phase 4）

#### T4.A.1 生成 BGM 音频
- **状态**: ✅
- **产物**: `Audio/BGM/BattleTheme.wav`, `Audio/Menu/MenuTheme.wav`, `Audio/BGM/BossTheme.wav`
- **说明**: 音频生成器输出格式为 WAV（非 MP3），Unity 中均可正常使用
- **验收**: 3首 BGM 资产存在

#### T4.A.2 生成武器音效
- **状态**: ✅
- **产物**: `Audio/SFX/WeaponSword.mp3`, `WeaponKnife.mp3`, `WeaponShield.mp3`, `WeaponEnergy.mp3`, `WeaponHoly.mp3`, `WeaponWater.mp3`
- **说明**: 武器 SFX 格式为 MP3（非 WAV），Unity 中均可正常使用
- **验收**: 6种武器音效资产存在

#### T4.A.3 生成敌人与环境音效
- **状态**: ✅
- **产物**: `Audio/SFX/EnemyHit.mp3`, `EnemyDie.wav`, `PlayerHit.wav`, `ExpPickup.wav`, `LevelUp.wav`, `Evolve.wav`
- **说明**: EnemyHit 为 MP3，其余为 WAV，格式混合但不影响 Unity 使用
- **验收**: 6种音效资产存在

### T4.2 音效系统集成

#### T4.2.1 实现 AudioManager 音效管理器
- **状态**: ✅
- **依赖**: T4.A.1, T4.A.2, T4.A.3
- **产物**: `Scripts/Core/AudioManager.cs`
- **说明**: Singleton<AudioManager>，BGM 双 AudioSource 交叉淡入淡出，SFX 12 音源轮转池 + 按 Clip ID 节流（0.05s）；订阅 GameEvents 自动触发（OnBossSpawned/Died→BGM切换，OnPlayerDamaged→PlayerHit SFX，OnPlayerLevelUp→LevelUp SFX，OnWeaponEvolved→Evolve SFX，OnEnemyDied→EnemyDie SFX，OnEnemyHit→EnemyHit SFX，OnDropCollected→ExpPickup SFX）
- **验收**: 编译通过；BGM切换和SFX播放可用；音量控制可用

#### T4.2.2 集成 BGM 播放
- **状态**: ✅
- **依赖**: T4.2.1
- **产物**: 更新 `Scripts/Core/GameManager.cs`
- **说明**: GameManager.StartGame()→AudioManager.PlayBattleBGM()；GameManager.ReturnToMenu()→AudioManager.PlayMenuBGM()；AudioManager.Start()检查当前状态播放对应BGM；OnBossSpawned→PlayBossBGM；OnBossDied→PlayBattleBGM
- **验收**: 菜单播放MenuTheme；战斗播放BattleTheme；BOSS出现切换BossTheme

#### T4.2.3 集成武器音效
- **状态**: ✅
- **依赖**: T4.2.1, T2.1.5
- **产物**: 更新 `Scripts/Data/WeaponData.cs`（新增 sfxClip 字段），更新 `Scripts/Weapons/WeaponBase.cs`（Attack 后播放 sfxClip），12 个 WeaponData SO 已分配 sfxClip
- **说明**: WeaponData 新增 public AudioClip sfxClip；WeaponBase.Update() 在 Attack() 后调用 AudioManager.PlaySFX(_data.sfxClip)；6 基础武器 + 6 进化武器均已分配对应 SFX
- **验收**: 武器攻击时播放对应音效

#### T4.2.4 集成敌人与环境音效
- **状态**: ✅
- **依赖**: T4.2.1
- **产物**: 更新 `Scripts/Core/GameEvents.cs`（新增 OnEnemyHit 事件），更新 `Scripts/Enemies/EnemyBase.cs`（TakeDamage 触发 OnEnemyHit）
- **说明**: 新增 GameEvents.OnEnemyHit + InvokeEnemyHit；EnemyBase.TakeDamage() 调用 GameEvents.InvokeEnemyHit(this)；AudioManager 订阅 OnEnemyHit/OnEnemyDied/OnPlayerDamaged/OnPlayerLevelUp/OnWeaponEvolved/OnDropCollected 自动播放对应 SFX
- **验收**: 受击/死亡/拾取/升级/进化时播放对应音效

### T4.3 特效系统

#### T4.3.1 实现受击特效
- **状态**: ⬜
- **依赖**: T1.1.5
- **产物**: `Prefabs/VFX/HitEffect.prefab`, 更新 `Scripts/Player/PlayerHitbox.cs`
- **验收**: 玩家受击时显示红色闪烁/数字

#### T4.3.2 实现武器攻击特效
- **状态**: ⬜
- **依赖**: T1.1.5
- **产物**: `Prefabs/VFX/SwordSlash.prefab`, `KnifeTrail.prefab`, `EnergyExplosion.prefab`, `HolyGlow.prefab`, `WaterSplash.prefab`, `ShieldImpact.prefab`
- **验收**: 各武器攻击时有对应视觉效果

#### T4.3.3 实现敌人死亡特效
- **状态**: ⬜
- **依赖**: T1.1.5
- **产物**: `Prefabs/VFX/EnemyDeath.prefab`, 更新 `Scripts/Enemies/EnemyBase.cs`
- **验收**: 敌人死亡时播放消散/爆炸特效

#### T4.3.4 实现经验拾取特效
- **状态**: ⬜
- **依赖**: T1.1.5
- **产物**: `Prefabs/VFX/ExpPickupEffect.prefab`, 更新 `Scripts/Drops/DropBase.cs`
- **验收**: 拾取经验宝石时显示吸收光效

### T4.4 数值平衡

#### T4.4.1 调整各武器伤害/冷却/升级曲线
- **状态**: ⬜
- **依赖**: T2.1.5-T2.1.9
- **产物**: 更新各 WeaponData SO
- **验收**: 武器强度梯度合理；飞刀高频低伤，能量球低频高伤

#### T4.4.2 调整敌人 HP/生成频率
- **状态**: ⬜
- **依赖**: T2.2.8, T2.6.1
- **产物**: 更新各 EnemyData SO 和 DifficultyManager 参数
- **验收**: 前5分钟无压力；10分钟有挑战；25分钟极限

#### T4.4.3 调整经验值需求曲线
- **状态**: ⬜
- **依赖**: T1.2.1
- **产物**: 更新 `Scripts/Player/PlayerStats.cs` 经验公式
- **验收**: 约30级对应30分钟；前期升级快后期慢

#### T4.4.4 调整进化武器强度
- **状态**: ⬜
- **依赖**: T2.4.3
- **产物**: 更新进化武器 WeaponData
- **验收**: 进化武器显著强于8级原版；但不破坏游戏节奏

#### T4.4.5 调整永久升级费用
- **状态**: ⬜
- **依赖**: T4.1.3
- **产物**: 更新 PermanentUpgradeData
- **验收**: 费用递增合理（50/100/200/400/800）；单局金币收入约100-300

#### T4.4.6 多轮测试迭代
- **状态**: ⬜
- **依赖**: T4.4.1-T4.4.5
- **验收**: 完整30分钟游戏体验流畅；难度曲线合理；无数值断层

### T4.5 性能优化

#### T4.5.1 对象池全量覆盖审计
- **状态**: ⬜
- **依赖**: T1.1.4, T1.1.5
- **产物**: 审计报告 + 修复
- **验收**: 运行时零 Instantiate/Destroy 调用（除首次创建）

#### T4.5.2 敌人 LOD 系统
- **状态**: ⬜
- **依赖**: T1.4.1
- **产物**: `Scripts/Enemies/EnemyLOD.cs`
- **验收**: 远处敌人5帧更新一次位置；近处每帧更新

#### T4.5.3 碰撞 Layer Matrix 优化
- **状态**: ⬜
- **依赖**: T1.2.5
- **产物**: 更新 Project Settings → Physics 2D Layer Matrix
- **验收**: 仅必要Layer之间有碰撞检测；敌人之间不碰撞

#### T4.5.4 Sprite 合批设置
- **状态**: ⬜
- **依赖**: T1.A
- **产物**: 更新 SpriteRenderer 和 SortingLayer 设置
- **验收**: 相同材质 Sprite 合批渲染；DrawCall 数量合理

#### T4.5.5 内存分配审计
- **状态**: ⬜
- **验收**: 消除 Update 中的临时 List/Array 分配；避免 LINQ 和闭包；无 GC Spikes

#### T4.5.6 Profiler 性能分析
- **状态**: ⬜
- **验收**: 使用 Profiler 定位热点；优化 Top 3 耗时函数

#### T4.5.7 目标验证：500敌人@60FPS
- **状态**: ⬜
- **依赖**: T4.5.1-T4.5.6
- **验收**: 500个敌人在场景中稳定60FPS运行

### T4.6 Bug 修复与打磨

#### T4.6.1 边界情况处理
- **状态**: 🔧
- **验收**: 处理：玩家死亡时武器仍在攻击；升级界面重复弹出；敌人重叠卡住等

#### T4.6.1a 玩家超出地图边界
- **状态**: ✅
- **问题**: PlayerController 无位置钳制，玩家可移出 ±100 地图边界，CameraFollow 钳制后导致相机不同步
- **解决**: PlayerController.FixedUpdate() 添加 Mathf.Clamp 钳制位置到 MapManager.CurrentMapHalfSize；MapManager 新增静态属性 CurrentMapHalfSize
- **涉及文件**: `Scripts/Player/PlayerController.cs`, `Scripts/Core/MapManager.cs`

#### T4.6.1b 飞刀/飞剑索敌偶尔无法锁定最近目标
- **状态**: ✅
- **问题**: SpatialGrid.QueryNearest() 查询前未调用 Reconcile()，敌人可能在过时格子中导致最近目标查询遗漏
- **解决**: 在 QueryNearest() 开头加 Reconcile() 调用，与 QueryInRadius() 保持一致
- **涉及文件**: `Scripts/Core/SpatialGrid.cs`

#### T4.6.1c HealthBar 订阅错误事件
- **状态**: ✅
- **问题**: PlayerStats.Heal() 即使满血也触发 OnPlayerHealed 事件（HolyLight 每 tick 调用），导致 HUD 在无实际 HP 变化时仍刷新
- **解决**: 先计算实际治疗量 actualHeal，仅在 > 0 时触发事件和累加 _totalHealed
- **涉及文件**: `Scripts/Player/PlayerStats.cs`

#### T4.6.1d 掉落数值硬编码重构
- **状态**: ✅
- **问题**: DropManager 的掉落率、经验宝石分级、回血值、磁铁配置等数值均为 [SerializeField] 硬编码或字面量 (health=30)，无法被策划在 Inspector 中独立调整
- **解决**: 新增 DropTableData ScriptableObject，集中管理所有掉落游戏数值；DropManager 改为引用 SO；DropBase 新增 SetMagnetConfig() 方法支持 SO 覆盖磁铁配置
- **产物**: `Scripts/Data/DropTableData.cs`, `Data/DropTableData.asset`
- **涉及文件**: `Scripts/Data/DropTableData.cs`, `Scripts/Drops/DropManager.cs`, `Scripts/Drops/DropBase.cs`

#### T4.6.2 UI 适配和交互优化
- **状态**: ⬜
- **验收**: 不同分辨率下 UI 正常显示；按钮点击响应正常；无穿透点击

#### T4.6.3 动画和过渡优化
- **状态**: ⬜
- **验收**: 升级界面有入场动画；BOSS出现警告动画；场景切换过渡

#### T4.6.4 教程/提示系统
- **状态**: ⬜
- **产物**: `Scripts/UI/TutorialUI.cs`
- **验收**: 首次游戏显示操作提示；新机制出现时显示说明

---

## 里程碑检查点

| 里程碑 | 验收标准 | 依赖任务 |
|--------|---------|----------|
| **M1 - 可玩原型** | Phase 1 完成；能移动→攻击→升级→死亡，核心循环可玩 | T1.1-T1.8 |
| **M2 - 玩法完整** | Phase 2 完成；6武器+进化+6敌人+被动道具，有深度 | T2.1-T2.6 |
| **M3 - 内容充实** | Phase 3 完成；5角色+3BOSS+地图+完整UI | T3.1-T3.5 |
| **M4 - 可发布** | Phase 4 完成；存档+音效+优化+平衡，达到可发布状态 | T4.1-T4.6 |

---

## 任务统计

| Phase | 分类数 | 子任务数 | ✅完成 | 🔧进行中 | ⬜待做 |
|-------|--------|---------|--------|---------|--------|
| Phase 1 | 9 | 34 | 34 | 0 | 0 |
| Phase 1.A | 1 | 6 | 6 | 0 | 0 |
| Phase 1.8 (补充) | 1 | 8 | 8 | 0 | 0 |
| Phase 2 | 6 | 38 | 38 | 0 | 0 |
| Phase 2.A | 1 | 7 | 7 | 0 | 0 |
| Phase 3 | 5 | 20 | 20 | 0 | 0 |
| Phase 3.A | 1 | 4 | 4 | 0 | 0 |
| Phase 4 | 6 | 25 | 9 | 0 | 16 |
| Phase 4.A | 1 | 3 | 3 | 0 | 0 |
| **合计** | **31** | **145** | **125** | **0** | **20** |

---

*文档版本: v2.4 | 最后更新: 2026-05-09*

## 游戏体验优化日志

### 2026-04-30 中期成长优化

**问题**: 5-10级成长缓慢，拾取经验掉落困难

**解决方案**:
1. **增大基础拾取范围**: PickupRange 1 → 2.5，磁铁被动 +0.5/级 → +1.0/级
2. **延时真空吸附**: 经验宝石落地2.5秒后自动从8距离吸引向玩家，带二次缓入加速效果 (t²)
3. **调整经验曲线**: `5+5×Lv²` → `5+3×Lv²`，5→6级 130→80，10→11级 505→305

**涉及文件**:
- `Scripts/Player/PlayerStats.cs` (PickupRange, EXPToNextLevel)
- `Scripts/Drops/DropBase.cs` (vacuum delay/range/acceleration)
- `Data/Passives/Magnet.asset` (effectPerLevel 0.5→1)

### 2026-04-30 武器进化系统完善 + 升级UI增强

**问题1**: 进化武器在普通升级池中出现，无需满足进化条件即可获取
**解决**: WeaponData 新增 `isEvolutionOnly` 标记；UpgradeManager 生成新武器选项时过滤标记为 true 的武器；6个进化武器资产已设置标记

**问题2**: 升级卡片缺少图标，武器升级描述看不出具体变化
**解决**:
1. 生成并分配全部12个武器图标（5基础+6进化+飞剑已有）和10个被动道具图标
2. 武器升级描述改为对比格式：`DMG 10→12 | CD 1.5s→1.4s | +1 Projectile | +1 Pierce`
3. 新武器描述增加类型标签：`[Projectile]` `[Orbital]` `[Area]` `[Support]`
4. 满级时额外显示 `★ 特殊效果`

**涉及文件**:
- `Scripts/Data/WeaponData.cs` (新增 isEvolutionOnly 字段)
- `Scripts/Upgrades/UpgradeManager.cs` (过滤进化武器)
- `Scripts/Upgrades/UpgradeOption.cs` (GetLevelDescription/BuildNewWeaponDesc 重写)
- `Data/Weapons/*.asset` (12个武器图标 + isEvolutionOnly标记)
- `Data/Passives/*.asset` (10个被动道具图标)

### 2026-04-30 圣水(HolyWater) Bug修复 + 升级系统断裂引用修复

**问题1**: 圣水水潭永不过期、不跟随玩家重新生成
**根因**: `AreaWeapon.RefreshAreaEffect()` 对所有区域武器无条件重置 `_duration`，导致水潭(非跟随型)永不过期；`Attack()` 始终走 `RefreshAreaEffect()` 分支，水潭永远停留在首次创建位置
**解决**:
1. `Attack()` 分策略处理：光环类首次创建后刷新；水潭类每次攻击先销毁旧水潭再在新位置创建
2. `RefreshAreaEffect()` 仅对 `_followsPlayer=true` 重置 duration，水潭类只更新范围（处理升级）

**问题2**: Zone 预制体使用 `Sliced` drawMode 但精灵无 9-slice border
**解决**: 所有 5 个 Zone 预制体改为 `Simple` 模式；`HolyWater/HolyLight.SetupAreaEffect()` 改用 `transform.localScale` 控制尺寸

**问题3**: 升级系统完全失效（升级UI不弹出）
**根因**: 早期 PassiveData 资产存放在两个目录（`Assets/Data/Passives/` 和 `Assets/Scripts/Data/Passives/`），删除重复资产时因 Windows 大小写不敏感误删了带正确 ID 的文件，导致 `UpgradeManager._availablePassives` 和 `PlayerWeaponManager._allPassives` 中 8 个引用断裂为 null；`GenerateUpgradeOptions()` 遍历时对 null 调用方法抛出 NullReferenceException，整个升级流程崩溃
**解决**:
1. 为 `Assets/Scripts/Data/Passives/` 下 8 个 PassiveData 补全 id 字段（bone/bracer/codex/feather/heart/magnet/shell/wing）
2. 为 PowerUp 和 SpeedBoost 补全 id（`powerup`/`speedboost`）
3. 恢复 `UpgradeManager._availablePassives` 和 `PlayerWeaponManager._allPassives` 的断裂引用
4. `UpgradeManager.GenerateUpgradeOptions()` 增加 `if (pd == null) continue` / `if (wd == null) continue` null 防护

**涉及文件**:
- `Scripts/Weapons/AreaWeapon.cs` (Attack/RefreshAreaEffect 分策略重写)
- `Scripts/Weapons/HolyWater.cs` (SetupAreaEffect 改用 localScale)
- `Scripts/Weapons/HolyLight.cs` (SetupAreaEffect 改用 localScale)
- `Scripts/Upgrades/UpgradeManager.cs` (null 防护)
- `Prefabs/Weapons/*Zone.prefab` (drawMode: Sliced → Simple)
- `Scripts/Data/Passives/*.asset` (补全 8 个 id + PowerUp/SpeedBoost id)
- `Data/Passives/PowerUp.asset`, `SpeedBoost.asset` (补全 id)

### 2026-05-06 升级卡片被动道具预览基于实际角色属性

**问题**: 升级选项卡片中被动道具的效果数值预览使用硬编码默认值（如移速默认3、拾取范围1、HP 100、倍率1），而非玩家当前实际属性。每个角色初始属性不同（如法师HP70、骑士护甲2、游侠弹数+1），导致预览值与实际升级后数值不匹配。
**根因**: `PassiveUpgradeOption.FormatDescription()` 为 static 方法，无法访问 `PlayerStats` 实例，只能用硬编码默认值计算预览。
**解决**:
1. `FormatDescription()` 改为实例方法，通过构造函数传入的 `_stats` 字段读取玩家当前实际属性
2. 每个属性预览改为 `当前值 + 本级增量` 格式，例如移速预览 `_stats.MoveSpeed + bonus`
3. 移除所有硬编码默认值（3f, 1f, 100f 等），改为实时读取

**涉及文件**:
- `Scripts/Upgrades/UpgradeOption.cs` (FormatDescription: static→instance, 硬编码→_stats实际值)

### 2026-05-06 "回到菜单"按钮失效修复

**问题**: 暂停菜单"回到菜单"按钮点击后场景重载但不显示主菜单，游戏HUD停留在黑屏上。
**根因**: `MainMenuUI` 组件未序列化在场景文件中，也无脚本在运行时通过 `AddComponent` 创建它。该组件仅因编辑器中未保存的运行时添加而存在，`SceneManager.LoadScene()` 后不会重建。
**解决**: 在 `HUDController.Start()` 中添加自愈检查：若 Canvas 上缺少 `MainMenuUI` 组件则自动 `AddComponent<MainMenuUI>()`。

**涉及文件**:
- `Scripts/UI/HUDController.cs` (Start() 中添加 MainMenuUI 存在性检查和自动创建)

### 2026-05-08 Bug修复: 玩家边界/索敌/HealthBar + DropTableData SO重构

**Bug1: 玩家超出地图边界**
- **根因**: PlayerController 无位置钳制，EdgeCollider2D 边界墙对 trigger 类型的 PlayerHitbox 不产生物理阻挡；CameraFollow 钳制相机但不钳制玩家，导致相机不同步
- **解决**: PlayerController.FixedUpdate() 添加 Mathf.Clamp 到 MapManager.CurrentMapHalfSize；MapManager 新增静态属性供无引用访问

**Bug2: 飞刀/飞剑索敌偶尔无法锁定最近目标**
- **根因**: SpatialGrid.QueryNearest() 查询前未调用 Reconcile()，敌人可能在过时格子中；QueryInRadius() 有此调用但 QueryNearest 遗漏
- **解决**: 在 QueryNearest() 开头加 Reconcile() 调用

**Bug3: HealthBar 订阅错误事件**
- **根因**: PlayerStats.Heal() 即使满血也触发 OnPlayerHealed 事件（HolyLight 每 tick 调用），导致 HUD 在无实际 HP 变化时仍刷新
- **解决**: 先计算实际治疗量 actualHeal，仅在 > 0 时触发事件和累加 _totalHealed

**重构: DropTableData SO**
- **问题**: DropManager 的掉落率/经验分级/回血值/磁铁配置等数值均为 [SerializeField] 硬编码或字面量，无法独立调整
- **解决**: 新增 DropTableData SO 集中管理；DropManager 引用 SO 替代原字段；DropBase 新增 SetMagnetConfig() 支持 SO 覆盖

**涉及文件**:
- `Scripts/Player/PlayerController.cs` (添加位置钳制)
- `Scripts/Core/MapManager.cs` (新增 CurrentMapHalfSize)
- `Scripts/Core/SpatialGrid.cs` (QueryNearest 加 Reconcile)
- `Scripts/Player/PlayerStats.cs` (Heal 仅在有效时触发事件)
- `Scripts/Data/DropTableData.cs` (新增)
- `Scripts/Drops/DropManager.cs` (引用 DropTableData SO)
- `Scripts/Drops/DropBase.cs` (新增 SetMagnetConfig)
- `Scripts/UI/MainMenuUI.cs` (移除临时调试日志)
- `Scripts/UI/PauseMenuController.cs` (移除临时调试日志)

### 2026-04-30 圣水(HolyWater) 生成间隔与伤害间隔混淆修复

**问题**: 圣水水潭频繁在玩家脚下刷新（每0.5s销毁重建），伤害生成间隔与水潭生成间隔混用同一cooldown值
**根因**: `HolyWater.asset` 的 `cooldown=0.5` 被当作生成间隔，而0.5s实际是伤害tick间隔；`AreaWeapon.Attack()` 对非跟随型每次销毁旧水潭重建新水潭
**解决**:
1. `AreaWeapon.Attack()` 非跟随型：仅当 `_currentArea == null` 时创建水潭，已有则跳过（让水潭自然过期后下次CD再生成）
2. `HolyWater.asset` cooldown 调整为合理的生成间隔：Lv1-4 → 6s, Lv5-7 → 5s, Lv8 → 4s
3. 伤害tick间隔 `_tickInterval = 0.5f` 保持不变，与生成CD独立

**涉及文件**:
- `Scripts/Weapons/AreaWeapon.cs` (Attack 非跟随型逻辑：销毁重建→仅当无水潭时创建)
- `Data/Weapons/HolyWater.asset` (cooldown: 0.5→6/6/6/6/5/5/5/4)
