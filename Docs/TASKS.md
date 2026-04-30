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
- **状态**: ⬜
- **依赖**: T1.1.8, T2.3.1
- **产物**: 更新 `Scripts/Data/WeaponData.cs`（进化字段）+ 更新6个现有 WeaponData 的进化关联
- **验收**: 每把武器可配置 canEvolve, requiredPassive, evolvedWeapon

#### T2.4.2 实现宝箱掉落触发进化逻辑
- **状态**: ⬜
- **依赖**: T2.4.1, T1.5.1, T1.2.4
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（宝箱收集逻辑）, 更新 `Scripts/Player/PlayerWeaponManager.cs`（进化方法）
- **验收**: 拾取宝箱时检查进化条件；满足则替换为进化武器

#### T2.4.3 创建6把进化武器数据
- **状态**: ⬜
- **依赖**: T2.4.1
- **产物**: `Data/Weapons/Excalibur.asset` (圣剑), `InfiniteKnife.asset` (无限飞刀), `JudgementWheel.asset` (审判之轮), `VoidBlackHole.asset` (虚空黑洞), `AngelsSong.asset` (天使之歌), `UndeadFlood.asset` (亡灵洪流)
- **验收**: 6个进化武器 SO 存在，包含完整等级数据

#### T2.4.4 创建进化武器 Prefab 和投射物
- **状态**: ⬜
- **依赖**: T2.4.3
- **产物**: 对应进化武器的 Prefab 和投射物 Prefab
- **验收**: 所有进化武器 Prefab 可正确实例化和攻击

#### T2.4.5 实现进化动画和特效
- **状态**: ⬜
- **依赖**: T2.4.2
- **产物**: `Scripts/Weapons/WeaponEvolutionVFX.cs`
- **验收**: 武器进化时播放闪光/变身特效

### T2.5 空间分区优化

#### T2.5.1 实现 SpatialGrid 空间分区网格
- **状态**: ⬜
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/SpatialGrid.cs`
- **验收**: 编译通过；Insert/Remove/Update/FindNearest/FindInRange/FindNearestN 可用

#### T2.5.2 武器瞄准使用空间分区查询
- **状态**: ⬜
- **依赖**: T2.5.1, T1.3.2
- **产物**: 更新 `Scripts/Weapons/ProjectileWeapon.cs`（替代 FindObjectsOfType）
- **验收**: 武器通过 SpatialGrid.FindNearest 查找目标

#### T2.5.3 敌人移动时更新网格位置
- **状态**: ⬜
- **依赖**: T2.5.1, T1.4.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`
- **验收**: 敌人每帧更新位置到 SpatialGrid

#### T2.5.4 性能验证
- **状态**: ⬜
- **依赖**: T2.5.2, T2.5.3
- **验收**: 500敌人场景下空间分区查询性能优于 FindObjectsOfType

### T2.6 难度曲线与角色解锁

#### T2.6.1 实现时间驱动的难度缩放系统
- **状态**: ⬜
- **依赖**: T1.1.6
- **产物**: `Scripts/Core/DifficultyManager.cs`
- **验收**: 编译通过；提供当前难度系数、敌人HP倍率、生成间隔等查询

#### T2.6.2 敌人HP随时间增长
- **状态**: ⬜
- **依赖**: T2.6.1, T1.4.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`
- **验收**: 敌人HP按 `baseHP × (1 + 0.1 × minutes)` 缩放

#### T2.6.3 生成密度和间隔随时间变化
- **状态**: ⬜
- **依赖**: T2.6.1, T1.4.2
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`
- **验收**: 生成间隔按 `baseInterval / (1 + 0.15 × minutes)` 递减

#### T2.6.4 精英波次定时出现
- **状态**: ⬜
- **依赖**: T2.6.1, T2.2.7
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`（精英波次逻辑）
- **验收**: 每5分钟出现一波精英敌人

#### T2.6.5 实现角色解锁条件系统
- **状态**: ⬜
- **依赖**: T1.2.1
- **产物**: `Scripts/Data/UnlockCondition.cs`, 更新 `Scripts/Data/CharacterData.cs`
- **验收**: 角色可配置解锁条件（存活时间/击杀数/治疗量）；游戏内可查询解锁状态

---

## Phase 3: 内容扩充 (Week 7-8)

### T3.1 多角色系统

#### T3.1.1 完善 CharacterData ScriptableObject
- **状态**: ⬜
- **依赖**: T1.1.8
- **产物**: 更新 `Scripts/Data/CharacterData.cs`
- **验收**: 包含 baseHP, moveSpeed, pickupRange, armor, startingWeapon, specialPassiveId, unlockCondition

#### T3.1.2 创建5个角色数据定义
- **状态**: ⬜
- **依赖**: T3.1.1
- **产物**: `Data/Characters/Hero.asset` (勇者:飞剑/HP+20%), `Mage.asset` (法师:能量球/冷却-10%), `Knight.asset` (骑士:旋转盾/护甲+2), `Ranger.asset` (游侠:飞刀/移速+15%), `Priest.asset` (牧师:圣光/再生+0.5s)
- **验收**: 5个角色 SO 存在，各属性差异化

#### T3.1.3 实现角色选择界面
- **状态**: ⬜
- **依赖**: T3.1.2
- **产物**: `Scripts/UI/CharacterSelectUI.cs`
- **验收**: 显示5个角色卡片；未解锁角色灰色+解锁条件提示；选中后开始游戏

#### T3.1.4 实现角色解锁逻辑
- **状态**: ⬜
- **依赖**: T2.6.5, T3.1.2
- **产物**: `Scripts/Core/UnlockManager.cs`
- **验收**: 游戏结算时检查解锁条件；达成条件弹出解锁提示

#### T3.1.5 生成其他4个角色 Sprite
- **状态**: ⬜
- **产物**: `Art/Sprites/Characters/Mage.png`, `Knight.png`, `Ranger.png`, `Priest.png`
- **验收**: 4个角色 Sprite 资产存在

### T3.2 BOSS 系统

#### T3.2.1 实现 BossEnemy 基类
- **状态**: ⬜
- **依赖**: T1.4.1
- **产物**: `Scripts/Enemies/BossEnemy.cs`
- **验收**: 编译通过；BOSS行为（多阶段、特殊攻击模式、血条显示）

#### T3.2.2 创建骷髅王 BOSS
- **状态**: ⬜
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Enemies/SkeletonKing.asset`, `Prefabs/Enemies/SkeletonKing.prefab`
- **验收**: 10分钟出现；HP=500；召唤骷髅群 + 范围震击攻击

#### T3.2.3 创建暗夜领主 BOSS
- **状态**: ⬜
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Enemies/DarkLord.asset`, `Prefabs/Enemies/DarkLord.prefab`
- **验收**: 20分钟出现；HP=2000；全屏弹幕 + 传送 + 召唤精英

#### T3.2.4 创建死神 BOSS
- **状态**: ⬜
- **依赖**: T3.2.1, T3.A.1
- **产物**: `Data/Enemies/Death.asset`, `Prefabs/Enemies/Death.prefab`
- **验收**: 30分钟出现；HP=5000；一击必杀线 + 追踪光束；不可被击杀（存活到时间结束）

#### T3.2.5 实现 BOSS 血条 UI
- **状态**: ⬜
- **依赖**: T3.2.1
- **产物**: `Scripts/UI/BossHealthBar.cs`
- **验收**: BOSS出现时屏幕顶部显示血条；HP实时更新

#### T3.2.6 更新 EnemySpawner 定时生成 BOSS
- **状态**: ⬜
- **依赖**: T3.2.2, T3.2.3, T3.2.4, T1.4.2
- **产物**: 更新 `Scripts/Enemies/EnemySpawner.cs`
- **验收**: 10/20/30分钟自动生成对应 BOSS

### T3.A 美术资产生成（Phase 3）

#### T3.A.1 生成 BOSS Sprites
- **状态**: ⬜
- **产物**: `Art/Sprites/Enemies/SkeletonKing.png`, `DarkLord.png`, `Death.png`
- **验收**: 3个 BOSS Sprite 资产存在

#### T3.A.2 生成地图环境 Sprites
- **状态**: ⬜
- **产物**: `Art/Sprites/Environment/Tree.png`, `Rock.png`, `Wall.png`, `Fence.png`
- **验收**: 环境装饰 Sprite 资产存在

#### T3.A.3 生成新掉落物 Sprites
- **状态**: ⬜
- **产物**: `Art/Sprites/Drops/RoastChicken.png`, `Chest.png`, `MagnetItem.png`
- **验收**: 3个掉落物 Sprite 资产存在

#### T3.A.4 生成 UI 界面 Sprites
- **状态**: ⬜
- **产物**: `Art/Sprites/UI/MenuBackground.png`, `CharacterCardFrame.png`, `BossBarFrame.png`
- **验收**: UI Sprite 资产存在

### T3.3 地图系统

#### T3.3.1 实现地面贴图/瓦片地图
- **状态**: ⬜
- **依赖**: T1.A.5
- **产物**: 更新 GameLevel 场景地面
- **验收**: 使用 Tilemap 或大贴图铺设200×200地图

#### T3.3.2 实现障碍物（树木、石头、墙壁）
- **状态**: ⬜
- **依赖**: T3.A.2
- **产物**: `Prefabs/Environment/Tree.prefab`, `Rock.prefab`, `Wall.prefab`
- **验收**: 障碍物有碰撞体；敌人无法穿过（幽灵除外）；投射物被阻挡

#### T3.3.3 实现边界围栏
- **状态**: ⬜
- **依赖**: T1.2.5
- **产物**: 更新 GameLevel 场景边界
- **验收**: 玩家和敌人无法离开200×200地图区域

#### T3.3.4 环境装饰
- **状态**: ⬜
- **依赖**: T3.A.2, T3.3.2
- **产物**: 地图中的装饰物件（草丛、花、碎石等）
- **验收**: 地图有视觉层次和丰富度

### T3.4 完整 UI

#### T3.4.1 实现主菜单/大厅界面
- **状态**: ⬜
- **依赖**: T3.A.4
- **产物**: `Scripts/UI/MainMenu.cs`, `Scenes/MainMenu.unity`
- **验收**: 包含开始游戏、角色选择、永久升级、图鉴、设置入口

#### T3.4.2 实现角色选择界面
- **状态**: ⬜
- **依赖**: T3.1.3, T3.A.4
- **产物**: 完善 `Scripts/UI/CharacterSelectUI.cs`
- **验收**: 显示5角色卡片，选中高亮，未解锁灰色+条件提示

#### T3.4.3 实现暂停菜单
- **状态**: ⬜
- **依赖**: T1.1.6
- **产物**: `Scripts/UI/PauseMenu.cs`
- **验收**: ESC键暂停；继续/设置/返回主菜单选项

#### T3.4.4 完善结算界面
- **状态**: ⬜
- **依赖**: T1.8.2
- **产物**: 更新 `Scripts/UI/ResultScreen.cs`
- **验收**: 显示存活时间/击杀数/获得经验/获得金币；解锁提示；再来一局/返回大厅按钮

#### T3.4.5 实现武器/角色图鉴界面
- **状态**: ⬜
- **依赖**: T2.3.3, T3.1.2
- **产物**: `Scripts/UI/CodexUI.cs`
- **验收**: 可浏览所有武器和角色信息；进化路线图

### T3.5 更多掉落物

#### T3.5.1 实现烤鸡掉落物
- **状态**: ⬜
- **依赖**: T1.5.1, T3.A.3
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Health类型）, `Prefabs/Drops/RoastChicken.prefab`
- **验收**: 敌人5%几率掉落；拾取恢复30HP

#### T3.5.2 实现宝箱掉落物
- **状态**: ⬜
- **依赖**: T2.4.2, T3.A.3
- **产物**: `Prefabs/Drops/Chest.prefab`
- **验收**: 精英/BOSS掉落；拾取触发武器进化（条件满足时）

#### T3.5.3 实现磁铁道具掉落
- **状态**: ⬜
- **依赖**: T1.5.1, T3.A.3
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Magnet类型）, `Prefabs/Drops/MagnetItem.prefab`
- **验收**: 拾取后临时增加拾取范围10秒

#### T3.5.4 完善金币掉落物
- **状态**: ⬜
- **依赖**: T1.5.1
- **产物**: 更新 `Scripts/Drops/DropBase.cs`（Gold收集逻辑）
- **验收**: 金币正确计入局外货币

#### T3.5.5 实现经验宝石分级
- **状态**: ⬜
- **依赖**: T1.5.1
- **产物**: `Prefabs/Drops/ExpGemSmall.prefab` (+1), `ExpGemMedium.prefab` (+5), `ExpGemLarge.prefab` (+20)
- **验收**: 普通/精英/BOSS掉落不同等级经验宝石

---

## Phase 4: 元进度与打磨 (Week 9-10)

### T4.1 存档与元进度

#### T4.1.1 实现 SaveManager 存档管理器
- **状态**: ⬜
- **依赖**: T1.1.1
- **产物**: `Scripts/Core/SaveManager.cs`
- **验收**: 编译通过；JSON序列化存取存档数据；Save/Load/DeleteSave 可用

#### T4.1.2 实现金币系统
- **状态**: ⬜
- **依赖**: T4.1.1, T3.5.4
- **产物**: `Scripts/Core/GoldManager.cs`
- **验收**: 局内金币收集 → 局外可查看余额；跨局持久化

#### T4.1.3 实现永久升级商店
- **状态**: ⬜
- **依赖**: T4.1.2
- **产物**: `Scripts/UI/UpgradeShopUI.cs`, `Scripts/Data/PermanentUpgradeData.cs`
- **验收**: 大厅中可花费金币购买永久加成（HP/移速/伤害/拾取/额外生命）；费用递增

#### T4.1.4 实现解锁系统
- **状态**: ⬜
- **依赖**: T4.1.1, T3.1.4
- **产物**: 更新 `Scripts/Core/UnlockManager.cs`（存档集成）
- **验收**: 角色和武器解锁状态持久化；新游戏可使用已解锁角色

#### T4.1.5 实现统计数据追踪
- **状态**: ⬜
- **依赖**: T4.1.1
- **产物**: `Scripts/Core/StatsTracker.cs`
- **验收**: 追踪总击杀数/总游戏次数/最长存活时间/总金币等；跨局持久化

### T4.A 音效资产生成（Phase 4）

#### T4.A.1 生成 BGM 音频
- **状态**: ⬜
- **产物**: `Audio/BGM/BattleTheme.mp3`, `MenuTheme.mp3`, `BossTheme.mp3`
- **验收**: 3首 BGM 资产存在

#### T4.A.2 生成武器音效
- **状态**: ⬜
- **产物**: `Audio/SFX/WeaponSword.wav`, `WeaponKnife.wav`, `WeaponShield.wav`, `WeaponEnergy.wav`, `WeaponHoly.wav`, `WeaponWater.wav`
- **验收**: 6种武器音效资产存在

#### T4.A.3 生成敌人与环境音效
- **状态**: ⬜
- **产物**: `Audio/SFX/EnemyHit.wav`, `EnemyDie.wav`, `PlayerHit.wav`, `ExpPickup.wav`, `LevelUp.wav`, `Evolve.wav`
- **验收**: 6种音效资产存在

### T4.2 音效系统集成

#### T4.2.1 实现 AudioManager 音效管理器
- **状态**: ⬜
- **依赖**: T4.A.1, T4.A.2, T4.A.3
- **产物**: `Scripts/Core/AudioManager.cs`
- **验收**: 编译通过；BGM切换和SFX播放可用；音量控制可用

#### T4.2.2 集成 BGM 播放
- **状态**: ⬜
- **依赖**: T4.2.1
- **产物**: 更新 `Scripts/Core/GameManager.cs`, `Scripts/UI/MainMenu.cs`
- **验收**: 菜单播放MenuTheme；战斗播放BattleTheme；BOSS出现切换BossTheme

#### T4.2.3 集成武器音效
- **状态**: ⬜
- **依赖**: T4.2.1, T2.1.5
- **产物**: 更新各 WeaponBase 子类
- **验收**: 武器攻击时播放对应音效

#### T4.2.4 集成敌人与环境音效
- **状态**: ⬜
- **依赖**: T4.2.1
- **产物**: 更新 `Scripts/Enemies/EnemyBase.cs`, `Scripts/Player/PlayerHitbox.cs`, `Scripts/Drops/DropBase.cs`, `Scripts/Upgrades/UpgradeManager.cs`
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
- **状态**: ⬜
- **验收**: 处理：玩家死亡时武器仍在攻击；升级界面重复弹出；敌人重叠卡住等

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
| Phase 1.8 (补充) | 1 | 5 | 5 | 0 | 0 |
| Phase 2 | 6 | 38 | 25 | 0 | 13 |
| Phase 2.A | 1 | 7 | 7 | 0 | 0 |
| Phase 3 | 5 | 20 | 0 | 0 | 20 |
| Phase 3.A | 1 | 4 | 0 | 0 | 4 |
| Phase 4 | 6 | 25 | 0 | 0 | 25 |
| Phase 4.A | 1 | 3 | 0 | 0 | 3 |
| **合计** | **31** | **142** | **76** | **0** | **66** |

---

*文档版本: v1.5 | 最后更新: 2026-04-30*

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
