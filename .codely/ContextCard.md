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
- GameEvents static bus (OnEnemyDied, OnPlayerLevelUp, OnDifficultyChanged, OnGamePaused/Resumed, etc.)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary; WeaponData.spreadAngle drives fan spacing
- BossEnemy abstract → ExecuteAttack, OnPhaseChanged; Destroy-on-death (not pooled)
- Factory: WeaponData.WeaponType → component creation

## Interfaces
- WeaponBase: cooldown, auto-attack, level stats; spreadAngle (per-weapon fan degree)
- Projectile: transform.up=_direction (blade tip forward for swords/daggers)
- EnemyBase: chase, takeDamage, die→spawn drops
- BossEnemy: multi-phase (HP thresholds), abstract ExecuteAttack, unkillable option, health bar events
- DropBase: vacuum after 2.5s, collect on contact
- UpgradeOption: Evolution|WeaponUpgrade|NewWeapon|Passive
- IWeightedRandom: weighted selection for drops/spawns
- UnlockCondition: SurviveTime|KillCount|HealAmount|ReachLevel|KillElites

## Systems
- ✅ Player: WASD+Rigidbody2D, stats (HP/EXP/level/multipliers), 6 weapon slots, TotalHealed/EliteKillCount tracking, magnet buff (timer-based PickupRange boost)
- ✅ Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive
- ✅ Enemies: chase AI, spawner time-scaling, elite 5min (count ramps per wave), mage; Bat moveSpeed=2.2
- ✅ Bosses: BossEnemy base (multi-phase, health bar integration, destroy-on-death), 3 bosses (SkeletonKing/DarkLord/DeathBoss), BossProjectile/BossShockwave, BossHealthBar UI (hides on pause/player death, LateUpdate guard), EnemySpawner timed boss spawn (10/20/30 min)
- ✅ Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive; PassiveUpgradeOption preview uses actual PlayerStats (not hardcoded defaults)
- ✅ Drops: EXP/Gold/Health/Chest/Magnet, vacuum mechanic, DropManager (deferred queue, 6/frame budget, EXP/Gold merge, scene-reload safe); EXP gem tiering (time-based: small→medium≥8min→large≥18min); Chest from elite/boss; Magnet rare drop (10s buff)
- ✅ UI: HUD (slider value clamped to MaxHP), UpgradeUI, BossHealthBar (pause/death-aware), ResultScreen (9 stats incl. level/elites/healed/character), PauseMenu (+Codex button, fires OnGamePaused/Resumed), CodexUI (CanvasGroup overlay, Weapons/Characters tabs, programmatic)
- ✅ Map: MapManager procedural generation (±100 boundary, 50 trees/35 rocks/12 walls/200 fence posts/80 grass); CameraFollow orthographic clamping; obstacles scaled to 0.04 (smaller than player), sortingOrder=-2 (below characters/enemies)
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
- Phase 1: 48/48 ✅ | Phase 2: 45/45 ✅ | Phase 3: 26/26 ✅ (M3 complete) | Phase 4: 0/26
- Game flow: MainMenuUI → 创建/选择存档 → CharacterSelectUI → StartGame(character) → Playing → GameOver → Unlock check → Retry/Menu
- Scene reload: GameManager (DontDestroyOnLoad) survives, PendingAutoStart for retry auto-start

## Phase 3 Complete ✅
- T3.1 多角色系统: 5/5 ✅
  - CharacterData: id, description, isDefaultUnlocked, portrait, stats差异化
  - UnlockManager: PlayerPrefs per-slot, CheckUnlocks at game end, OnCharacterUnlocked event
  - CharacterSelectUI: 5 cards, portrait/stats/description, lock/unlock, detail panel
  - PlayerStats.InitializeFromCharacterData(), PlayerWeaponManager reads startingWeapon from CharacterData
  - GameManager.StartGame(): 显式初始化 PlayerStats + PlayerWeaponManager (修复 Awake/Start 时序)
  - GameManager.PendingAutoStart: 重试时保存角色, 场景重载后自动开始
- T3.A 美术资产生成(Phase 3): 4/4 ✅ (BOSS/环境/掉落物/UI sprites)
- T3.2 BOSS 系统: 6/6 ✅ (BossEnemy base + 3 bosses + BossHealthBar + timed spawn)
- T3.3 地图系统: 4/4 ✅
  - MapManager: procedural ±100 boundary (EdgeCollider2D), 50 trees/35 rocks/12 walls/200 fence/80 grass
  - CameraFollow: orthographic clamping within map bounds
  - Obstacles: SpriteRenderer+BoxCollider2D, random rotation/scale, 10-unit clear radius from center; scaleMultiplier=0.04f, sortingOrder=-2 (below characters)
- T3.4 完整UI: 3/3 ✅ (T3.4.1 MainMenu done earlier)
  - T3.4.3 PauseMenu: +Codex button (ShowCodex→CodexUI.Show)
  - T3.4.4 ResultScreen: 9 stats (level/elites/healed/character + original 5), programmatic rebuild
  - T3.4.5 CodexUI: CanvasGroup overlay, Weapons/Characters tabs, WeaponData/CharacterData browsing, ScrollRect
- T3.5 更多掉落: 5/5 ✅
  - DropType.Magnet + Chest; DropBase.Collect() handles magnet→PlayerStats.ApplyMagnetEffect
  - PlayerStats: magnet buff timer (10s PickupRange boost, auto-revert)
  - DropManager: chest (elite/boss only), magnet (rare), EXP gem tiering (small 1×/medium 5× at ≥8min/large 20× at ≥18min)
  - 3 new prefabs: RoastChicken, Chest, MagnetItem (all scale 0.08, CircleCollider2D trigger)
- T3.4.1 MainMenu + T4.1.1 Save: done earlier (partial, tracked in Phase 4)

## Phase 2 Complete ✅
- SpatialGrid O(1) queries, DifficultyManager time-driven scaling, Elite waves, 5 UnlockCondition types, 8 Chinese-named passives w/ evolution links

## Difficulty Scaling Formulas
- HPMultiplier = 1 + 0.1 × minutes
- SpawnIntervalMultiplier = 1 / (1 + 0.15 × minutes)
- DamageMultiplier = 1 + 0.05 × minutes (PlayerHitbox + MageEnemy)
- SpeedMultiplier = 1 + 0.02 × minutes (EnemyBase.Initialize)

## Key Decisions
- Built-in RP: simpler 2D, fewer shader compat issues
- SO data-driven: designers tweak without recompile
- Static events: zero-coupling, any system can subscribe
- Tuanjie over Unity: project originated on this engine fork
- AreaWeapon split strategy: _followsPlayer auras refresh duration; puddles only create when none exists, expire naturally, respawn on next CD
- DifficultyManager on GameManager GO: shares lifecycle, resets on StartGame()
- MapManager procedural: no env prefabs needed, everything built in Start(); obstacles use scaleMultiplier param for consistent sizing below characters
- EXP gem tiering: time-based thresholds (8min/18min) + enemy type (elite/boss boost)
- CodexUI: CanvasGroup overlay, programmatic construction (no prefab), Resources.FindObjectsOfTypeAll for data

## Known Issues
- MainMenuUI not serialized in scene; HUDController.Start() auto-creates it via AddComponent if missing

## Pitfalls
- GameEvents.ClearAll() in StartGame() wiped UpgradeManager/HUDController/ResultScreen event subscriptions → removed, scene reload handles cleanup
- DontDestroyOnLoad singletons survive SceneManager.LoadScene → must reset state via ReturnToMenu() before reload
- PlayerStats.Awake/PlayerWeaponManager.Start run before character selection → GameManager.StartGame() must explicitly call InitializeFromCharacterData() and EquipStartingWeapon()
- MainMenuUI.Awake() must check GameManager.CurrentState — if already Playing (auto-start after restart), skip Show() to avoid timeScale=0 / HUD hidden
- DropManager (DontDestroyOnLoad) must re-register pools + clear _pending on sceneLoaded, since Awake() only runs once
- Heal() must fire OnPlayerHealed event; HUDController.RefreshHPIfNeeded() must use float comparison (not int cast) to catch fractional HP changes
- PassiveEffect.Remove for MaxHP must call ClampCurrentHP() — otherwise CurrentHP > MaxHP causes slider >100% fill
- BossHealthBar must hide on pause (OnGamePaused) and player death (OnPlayerDied); LateUpdate guard ensures hidden when no valid boss
- Drop prefabs (RoastChicken, Chest, MagnetItem) must use scale (0.08,0.08,1) matching ExpGem/GoldCoin, and have CircleCollider2D
