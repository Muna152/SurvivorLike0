# SurvivorLike0 ContextCard

## Engine
- Tuanjie 1.8.4 | Built-in RP | .NET Standard 2.1 | 2D top-down

## Structure
- Scripts/: Player|Enemies|Weapons|Upgrades|Drops|UI|Core|Data
- Data/: SO defs (Weapon|Passive|Enemy|Character*Data)
- Prefabs/: mirror Scripts/ | Art/ Audio/ Fonts/

## Conventions
- Class: PascalCase | Private: _camelCase | Public field: PascalCase
- SO suffix: *Data | Base class: *Base | Folder-by-feature

## Prohibitions
- No URP/HDRP shaders (Built-in RP only)
- No Find()/FindObjectOfType() in Update/LateUpdate
- No MonoBehaviour on data-only classes (use SO)
- No coroutines for visual effects (use timed callbacks)
- No per-frame UI polling (use events)

## Patterns
- Singleton<T>: GameManager, PoolManager, DropManager, DifficultyManager, UnlockManager
- Static utility: SaveSlotManager (no MonoBehaviour, pure PlayerPrefs)
- ObjectPool<T> w/ prewarm & HashSet tracking
- GameEvents static bus (OnEnemyDied, OnPlayerLevelUp, OnDifficultyChanged, etc.)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary
- BossEnemy abstract → ExecuteAttack, OnPhaseChanged; Destroy-on-death (not pooled)
- Factory: WeaponData.WeaponType → component creation

## Interfaces
- WeaponBase: cooldown, auto-attack, level stats
- EnemyBase: chase, takeDamage, die→spawn drops
- BossEnemy: multi-phase (HP thresholds), abstract ExecuteAttack, unkillable option, health bar events
- DropBase: vacuum after 2.5s, collect on contact
- UpgradeOption: Evolution|WeaponUpgrade|NewWeapon|Passive
- IWeightedRandom: weighted selection for drops/spawns
- UnlockCondition: SurviveTime|KillCount|HealAmount|ReachLevel|KillElites

## Systems
- ✅ Player: WASD+Rigidbody2D, stats (HP/EXP/level/multipliers), 6 weapon slots, TotalHealed/EliteKillCount tracking
- ✅ Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive
- ✅ Enemies: chase AI, spawner time-scaling, elite 5min (count ramps per wave), mage; Bat moveSpeed=2.2
- ✅ Bosses: BossEnemy base (multi-phase, health bar integration, destroy-on-death), 3 bosses (SkeletonKing/DarkLord/DeathBoss), BossProjectile/BossShockwave, BossHealthBar UI, EnemySpawner timed boss spawn (10/20/30 min)
- ✅ Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive; PassiveUpgradeOption preview uses actual PlayerStats (not hardcoded defaults)
- ✅ Drops: EXP/Gold/Health, vacuum mechanic, DropManager (deferred queue, 6/frame budget, EXP/Gold merge within 2.5f radius, scene-reload safe)
- ✅ UI: HUD (HP/EXP/timer/weapon bar, float-based HP change detection, OnPlayerHealed event), result screen, UpgradeUI, BossHealthBar (event-driven, top of screen)
- ✅ Difficulty: DifficultyManager drives HP/Speed/Damage/SpawnInterval multipliers over time
- ✅ Unlock: UnlockCondition struct on CharacterData, runtime IsUnlocked() check, slot-aware PlayerPrefs
- ✅ Character System: 5 characters (Hero/Mage/Knight/Ranger/Priest), CharacterSelectUI, UnlockManager w/ per-slot PlayerPrefs
- ✅ Main Menu: MainMenuUI w/ Start/Quit buttons, save slot management panel (3 slots, create/delete/switch)
- ✅ Save System: SaveSlotManager (static, PlayerPrefs-based), slot-isolated unlock data

## Perf Budget
- Max 500 enemies on-screen | 6 weapon limit | Weapon max level 8
- Pool: enemies, projectiles, drops (all prewarmed)
- Event-driven UI | Cached refs | Reusable lists | Min GC alloc
- SpatialGrid: O(1) proximity queries, cell size 8f

## State
- Phase 1: 48/48 ✅ | Phase 2: 45/45 ✅ | Phase 3-4: 18/52 (T3.1+T3.A+T3.2 done, T3.4.1/T4.1.1 partial)
- Game flow: MainMenuUI → 创建/选择存档 → CharacterSelectUI → StartGame(character) → Playing → GameOver → Unlock check → Retry/Menu
- Scene reload: GameManager (DontDestroyOnLoad) survives, PendingAutoStart for retry auto-start

## Phase 3-4 Progress
- T3.1 多角色系统: 5/5 ✅
  - CharacterData: id, description, isDefaultUnlocked, portrait, stats差异化
  - UnlockManager: PlayerPrefs per-slot, CheckUnlocks at game end, OnCharacterUnlocked event
  - CharacterSelectUI: 5 cards, portrait/stats/description, lock/unlock, detail panel, "返回主菜单" button
  - PlayerStats.InitializeFromCharacterData(), PlayerWeaponManager reads startingWeapon from CharacterData
  - GameManager.StartGame() 显式初始化 PlayerStats + PlayerWeaponManager (修复 Awake/Start 时序)
  - GameManager.PendingAutoStart: 重试时保存角色, 场景重载后自动开始
  - 暂停菜单新增 重新开始/回到菜单 按钮, 逻辑同 ResultScreen
- T3.A 美术资产生成(Phase 3): 4/4 ✅
  - T3.A.1 BOSS: SkeletonKing.png, DarkLord.png, Death.png → Art/Sprites/Enemies/
  - T3.A.2 环境: Tree.png, Rock.png, Wall.png, Fence.png → Art/Sprites/Environment/
  - T3.A.3 掉落物: RoastChicken.png, Chest.png, MagnetItem.png → Art/Sprites/Drops/
  - T3.A.4 UI: MenuBackground.png(透明), CharacterCardFrame.png, BossBarFrame.png → Art/Sprites/UI/
  - 所有 Sprite 从 TJGenerators/History 整理到 Assets/Art/Sprites/ 对应子目录
  - 旧 Assets/Sprites/ 目录已清理，统一使用 Assets/Art/Sprites/ 结构
- T3.2 BOSS 系统: 6/6 ✅
  - BossEnemy: 抽象基类 (EnemyBase), 多阶段系统, 血条事件集成, 增强掉落, Destroy-on-death (非池化)
  - SkeletonKing: 10分钟, HP=500, 召唤骷髅群 + 震击攻击, 50%血量阶段转换
  - DarkLord: 20分钟, HP=2000, 扇形弹幕 + 环形弹幕 + 传送 + 召唤精英, 两阶段转换
  - DeathBoss: 30分钟, HP=5000, 追踪弹幕 + 扩散攻击, 不可击杀 (_isUnkillable), 存活到时间结束
  - BossProjectile: 通用弹幕组件 (damage/speed/direction/pierce)
  - BossShockwave: 扩展AoE震击环 (expandSpeed/maxRadius/damage)
  - BossHealthBar: 编程式构建, 事件驱动 (OnBossSpawned/Died/HealthChanged), 顶部显示
  - GameEvents: +OnBossSpawned, OnBossDied, OnBossHealthChanged
  - EnemySpawner: +CheckBossSpawns (10/20/30分钟), SpawnBoss, ResetBossFlags
  - EnemyBase: _originalColor/_flashTimer/_flashing 改为 protected (子类访问)
  - GameManager.StartGame(): 重置 spawner.ResetBossFlags()
  - 资产: Data/Bosses/*.asset, Prefabs/Bosses/*.prefab
- T3.4.1 主菜单界面: ✅ (partial)
  - MainMenuUI: 编程式构建，"开始游戏"/"退出游戏"按钮，左上角"存档管理"按钮
  - MainMenuUI 显示时隐藏 HUD 元素，切换到角色选择时恢复
  - CharacterSelectUI 由 MainMenuUI 控制显隐（不再自动显示）
- T4.1.1 存档系统: ✅ (partial)
  - SaveSlotManager: 静态工具类，管理3个存档栏位（PlayerPrefs）
  - 每个存档有字符串名称，不允许重名
  - 创建/删除/切换存档，切换时自动重载 UnlockManager
  - PlayerPrefs 键格式: SaveSlot_{i}_Name, Save_{i}_Unlock_{charId}
  - UnlockManager 改为 slot-aware：解锁数据按存档隔离

## Phase 2 Complete ✅
- T2.5 SpatialGrid: O(1) queries, Reconcile() sync, perf verified at 500 enemies
- T2.6 DifficultyManager: time-driven scaling (HP +10%/min, spawn accel +15%/min, damage +5%/min, speed +2%/min)
- T2.6 Elite waves: count = 5-10 + waveCount×2, high-tier enemy weight boost
- T2.6 UnlockCondition: 5 types (SurviveTime/KillCount/HealAmount/ReachLevel/KillElites)
- PassiveData dedup: removed old PowerUp/SpeedBoost (Assets/Data/Passives/), kept 8 Chinese-named passives with evolution links

## Difficulty Scaling Formulas
- HPMultiplier = 1 + 0.1 × minutes
- SpawnIntervalMultiplier = 1 / (1 + 0.15 × minutes)
- DamageMultiplier = 1 + 0.05 × minutes (applied in PlayerHitbox + MageEnemy)
- SpeedMultiplier = 1 + 0.02 × minutes (applied in EnemyBase.Initialize)

## Key Decisions
- Built-in RP: simpler 2D, fewer shader compat issues
- SO data-driven: designers tweak without recompile
- Static events: zero-coupling, any system can subscribe
- Tuanjie over Unity: project originated on this engine fork
- AreaWeapon split strategy: _followsPlayer auras refresh duration; puddles only create when none exists, expire naturally, respawn on next CD
- DifficultyManager on GameManager GO: shares lifecycle, resets on StartGame()

## Known Issues
- WeaponEvolutionVFX._particleCount unused (CS0414 warning, cosmetic only)
- MainMenuUI not serialized in scene; HUDController.Start() auto-creates it via AddComponent if missing
- BossEnemy.OnDisable hides EnemyBase.OnDisable (both do ActiveEnemies.Remove + SpatialGrid.Unregister; idempotent)
- HUD Canvas: 7 "referenced script missing" errors (pre-existing)
- BossHealthBar font: Arial.ttf invalid, needs LegacyRuntime.ttf (pre-existing)

## Pitfalls
- GameEvents.ClearAll() in StartGame() wiped UpgradeManager/HUDController/ResultScreen event subscriptions → removed, scene reload handles cleanup
- DontDestroyOnLoad singletons survive SceneManager.LoadScene → must reset state via ReturnToMenu() before reload
- PlayerStats.Awake/PlayerWeaponManager.Start run before character selection → GameManager.StartGame() must explicitly call InitializeFromCharacterData() and EquipStartingWeapon()
- MainMenuUI.Awake() must check GameManager.CurrentState — if already Playing (auto-start after restart), skip Show() to avoid timeScale=0 / HUD hidden
- DropManager (DontDestroyOnLoad) must re-register pools + clear _pending on sceneLoaded, since Awake() only runs once
- Heal() must fire OnPlayerHealed event; HUDController.RefreshHPIfNeeded() must use float comparison (not int cast) to catch fractional HP changes
