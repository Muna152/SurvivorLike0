# SurvivorLike0 ContextCard

## Engine & Conventions
- Tuanjie 1.8.4 | Built-in RP | .NET Standard 2.1 | 2D top-down
- PascalCase classes/fields | _camelCase private | SO suffix *Data | Base class *Base
- No URP shaders | No Find() in Update | No coroutines for VFX | No per-frame UI polling

## Architecture Patterns
- Singleton<T>: GameManager, PoolManager, DropManager, DifficultyManager, UnlockManager, AudioManager, VFXManager
- Static util: SaveSlotManager, GoldManager, StatsTracker (PlayerPrefs, no MonoBehaviour)
- Database SO: WeaponDatabase/EnemyDatabase/CharacterDatabase/PassiveDatabase — Resources.Load singleton w/ GetById(id), replace scene [SerializeField] arrays
- ObjectPool<T> w/ prewarm + HashSet tracking
- GameEvents static bus (OnEnemyDied/Hit/Damaged, OnPlayerDamaged/Healed/LevelUp/Died, OnBossSpawned/Died, OnDropCollected, OnWeaponEvolved, OnGoldChanged, OnGamePaused/Resumed, OnDifficultyChanged, OnPermanentUpgradePurchased, OnCharacterUnlocked)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary; WeaponData.WeaponType + AreaSubType factory
- BossEnemy abstract → ExecuteAttack, multi-phase, destroy-on-death (not pooled)
- DropBase: vacuum 2.5s, collect on contact; DropManager: deferred queue 6/frame, EXP/Gold merge

## Key Systems (all complete)
- Player: WASD+Rigidbody2D, 6 weapon slots, magnet buff, ExtraLives, position clamped
- Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive, VFXType enum
- Enemies: chase AI, spawner time-scaling, elite 5min, mage; EnemyData.id/icon added
- Bosses: 3 bosses (SkeletonKing/DarkLord/DeathBoss), BossHealthBar, timed spawn (10/20/30 min); EnemyDatabase.bosses[]
- Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive
- Drops: EXP/Gold/Health/Chest/Magnet, tiered EXP gems (8min/18min thresholds); Chest: pause+animation+evolution/upgrade (ChestOpenUI, OnChestCollected event), chestDropChance(elite 15%/boss 50%)
- Audio: BGM crossfade A/B, SFX 12-source round-robin, per-clip throttle 0.05s, GameEvents-driven
- VFX: VFXBase (scale+alpha), DamageNumber (TextMesh), 12 pooled prefabs in Resources/VFX/, GameEvents-driven
- Map: ±100 boundary, procedural obstacles (scale 0.04, sortOrder=-2)
- Difficulty: time-driven HP/Speed/Damage/SpawnInterval multipliers
- UI: HUD, UpgradeUI, BossHealthBar, ResultScreen (9 stats+gold), PauseMenu (4-btn), CodexUI, MainMenuUI, UpgradeShopUI, CharacterSelectUI, ChestOpenUI (VS-like pause+animation)
- Meta: 5 characters, 3-slot save, gold+5 permanent upgrades, stats tracking, unlock system

## Perf Budget
- 500 enemies max | 6 weapons | Weapon max level 8
- SpatialGrid O(1) queries (cell=8f, frame-stamped Reconcile skips if UpdateAll ran this frame)
- 500 enemies@153FPS verified (2026-05-13); QueryNearest 0.024ms, QueryInRadius 0.008ms
- DropBase uses sqrMagnitude (not Vector2.Distance/sqrt)
- Event-driven UI | Cached refs | Reusable lists | Min GC

## Data Architecture
- **Assets/Resources/Data/** = Content SOs + 4 Database SOs: Weapons/, Enemies/, Bosses/, Characters/, Passives/
- Database SOs: WeaponDatabase, EnemyDatabase (enemies+bosses), CharacterDatabase, PassiveDatabase — each Resources.Load singleton w/ GetById()
- All SOs have `id` field; lookup via Database.GetById(id) replaces scene [SerializeField] arrays
- **Assets/Resources/GameBalanceConfig.asset** = ALL system tuning params (single SO): difficulty scaling, elite/boss multipliers, drop rates, spawner params, weapon behavior, drop physics
- DifficultyManager/EnemySpawner/UpgradeManager read from GameBalanceConfig or Database SOs (no scene [SerializeField] for content arrays)

## State
- Phase 1-3: ✅ | Phase 4: 24/26 | Data architecture refactor: Assets/Data/ → Resources/Data/ + Database SOs
- Game flow: MainMenuUI → Save slot → Shop → CharacterSelect → StartGame → Playing → GameOver → Gold/Stats persisted → Retry/Menu
- Scene reload: DontDestroyOnLoad singletons survive; GameManager resets via ReturnToMenu()

## Balance Pass (2026-05-11)
- XP公式: 5+2×Lv² (原5+3×Lv²) → 30min可达Lv28-32
- 飞刀大幅削弱(8→5基础, 8→7弹Lv8); 旋转盾增强(5→8基础); 圣水大幅增强(3→5基础, 6→5s CD); 圣光增强(持续5→9s)
- 进化武器全部重做(Lv1≈原Lv8×1.5-1.8, Lv8≈原Lv8×2.5-3.0)
- 被动对齐GDD: Wing +8%/级(0.24flat), Feather 3级每级+1弹(maxLevel=3, effectPerLevel=1.0), Heart +12HP, Magnet +1.25拾取
- 敌人HP小幅上调, 伤害下调; Boss HP+60%, 伤害+4-10倍(骷髅王800/8, 暗夜领主3000/12, 死神8000/15)
- DifficultyManager缩放降低: HP +8%/min, 伤害+3%/min, 生成+12%/min, 速度+1.5%/min
- 掉落率提升: 烤鸡0.1→2%, 宝箱0.5→1.5%, 磁铁0.2→1%, EXP 80→75%
- 永久升级: 伤害费用减半(50-800), 拾取降低(20-320), 额外生命降低(300-1200)
- GameBalanceConfig SO (Resources/) 集中管理: 难度缩放(HP/Dmg/Speed/Spawn), 精英倍率(HP×8/Dmg×2/EXP×5), Boss缩放/阶段转换, AreaWeapon tick间隔, Orbital旋转速度/命中冷却, Projectile搜索/范围, 掉落率(EXP75%/烤鸡2%/宝箱1.5%/磁铁1%), EXP宝石分级(1/5/20, 8min/18min阈值), 回血30HP, 磁铁10s/+5拾取, 额外生命复活HP%, Spawner参数, 掉落物理(吸附/真空/合并)

## Remaining Phase 4 Tasks
- T4.4.6 数值平衡 playtest 迭代
- T4.5 性能优化 ✅ (2026-05-13: frame-stamped Reconcile, sqrMagnitude drops, elite reset fix, 500@153FPS verified)
- T4.6 收尾 (remaining: UI adaptation, animation transitions, tutorial system)

## Pitfalls (likely to bite again)
- GameEvents.ClearAll() removed — scene reload handles cleanup
- PlayerStats/PlayerWeaponManager init before character selection → GameManager.StartGame() must call InitializeFromCharacterData() + EquipStartingWeapon()
- MainMenuUI.Awake() must check GameManager.CurrentState — skip Show() if Playing
- DropBase.Collect() Chest type fires OnChestCollected event (does NOT directly call CheckAndEvolveWeapons); ChestOpenUI handles pause+animation+result
- Heal() fires OnPlayerHealed only on actual HP change; HUD uses float comparison
- PassiveEffect.Remove for MaxHP must ClampCurrentHP()
- BossHealthBar hides on pause/death; LateUpdate guard when no valid boss
- ResultScreen._persisted guard — reset in Retry/ReturnToMenu
- ReturnToMenu() must clear SelectedCharacter=null
- WeaponBase.Initialize() sets _cooldownTimer to first-level cooldown (not 0)
- ExtraLife revive returns 50% HP, skips OnPlayerDied
- BossEnemy.Die() does NOT call base.Die() — fires OnEnemyDied itself
- VFXManager loads from Resources/VFX/, handles scene reload via OnSceneLoaded
- DamageNumber uses TextMesh (MeshRenderer), no SpriteRenderer
- AudioManager SFX throttle 0.05s; BGM crossfade uses unscaledDeltaTime
- WeaponData.sfxClip/VFXType played from WeaponBase — subclasses inherit automatically
- EnemyBase.ResetForReuse() resets elite state (_isElite, _sr.color, _eliteDamageMultiplier)
- SpatialGrid.Reconcile() frame-stamped: skips O(n) sweep if UpdateAll() already ran this frame
