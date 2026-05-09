# SurvivorLike0 ContextCard

## Engine & Conventions
- Tuanjie 1.8.4 | Built-in RP | .NET Standard 2.1 | 2D top-down
- PascalCase classes/fields | _camelCase private | SO suffix *Data | Base class *Base
- No URP shaders | No Find() in Update | No coroutines for VFX | No per-frame UI polling

## Architecture Patterns
- Singleton<T>: GameManager, PoolManager, DropManager, DifficultyManager, UnlockManager, AudioManager, VFXManager
- Static util: SaveSlotManager, GoldManager, StatsTracker (PlayerPrefs, no MonoBehaviour)
- ObjectPool<T> w/ prewarm + HashSet tracking
- GameEvents static bus (OnEnemyDied/Hit/Damaged, OnPlayerDamaged/Healed/LevelUp/Died, OnBossSpawned/Died, OnDropCollected, OnWeaponEvolved, OnGoldChanged, OnGamePaused/Resumed, OnDifficultyChanged, OnPermanentUpgradePurchased, OnCharacterUnlocked)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary; WeaponData.WeaponType factory
- BossEnemy abstract → ExecuteAttack, multi-phase, destroy-on-death (not pooled)
- DropBase: vacuum 2.5s, collect on contact; DropManager: deferred queue 6/frame, EXP/Gold merge

## Key Systems (all complete)
- Player: WASD+Rigidbody2D, 6 weapon slots, magnet buff, ExtraLives, position clamped
- Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive, VFXType enum
- Enemies: chase AI, spawner time-scaling, elite 5min, mage; Bat speed=2.2
- Bosses: 3 bosses (SkeletonKing/DarkLord/DeathBoss), BossHealthBar, timed spawn (10/20/30 min)
- Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive
- Drops: EXP/Gold/Health/Chest/Magnet, tiered EXP gems (8min/18min thresholds), DropTableData SO
- Audio: BGM crossfade A/B, SFX 12-source round-robin, per-clip throttle 0.05s, GameEvents-driven
- VFX: VFXBase (scale+alpha), DamageNumber (TextMesh), 12 pooled prefabs in Resources/VFX/, GameEvents-driven
- Map: ±100 boundary, procedural obstacles (scale 0.04, sortOrder=-2)
- Difficulty: time-driven HP/Speed/Damage/SpawnInterval multipliers
- UI: HUD, UpgradeUI, BossHealthBar, ResultScreen (9 stats+gold), PauseMenu (4-btn), CodexUI, MainMenuUI, UpgradeShopUI, CharacterSelectUI
- Meta: 5 characters, 3-slot save, gold+5 permanent upgrades, stats tracking, unlock system

## Perf Budget
- 500 enemies max | 6 weapons | Weapon max level 8
- SpatialGrid O(1) queries (cell=8f, Reconcile() on all query paths)
- Event-driven UI | Cached refs | Reusable lists | Min GC

## State
- Phase 1-3: ✅ | Phase 4: 19/26
- Game flow: MainMenuUI → Save slot → Shop → CharacterSelect → StartGame → Playing → GameOver → Gold/Stats persisted → Retry/Menu
- Scene reload: DontDestroyOnLoad singletons survive; GameManager resets via ReturnToMenu()

## Remaining Phase 4 Tasks
- T4.4 数值平衡 (weapons/enemies/XP/evolution costs + playtesting)
- T4.5 性能优化 (pool audit, LOD, layers, batching, GC, profiler, 500 enemies@60FPS)
- T4.6 收尾 (remaining: UI adaptation, animation transitions, tutorial system)

## Pitfalls (likely to bite again)
- GameEvents.ClearAll() removed — scene reload handles cleanup
- PlayerStats/PlayerWeaponManager init before character selection → GameManager.StartGame() must call InitializeFromCharacterData() + EquipStartingWeapon()
- MainMenuUI.Awake() must check GameManager.CurrentState — skip Show() if Playing
- DropManager must re-register pools + clear _pending on sceneLoaded
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
