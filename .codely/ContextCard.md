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
- Singleton<T>: GameManager, PoolManager, DropManager, DifficultyManager
- ObjectPool<T> w/ prewarm & HashSet tracking
- GameEvents static bus (OnEnemyDied, OnPlayerLevelUp, OnDifficultyChanged, etc.)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary
- Factory: WeaponData.WeaponType → component creation

## Interfaces
- WeaponBase: cooldown, auto-attack, level stats
- EnemyBase: chase, takeDamage, die→spawn drops
- DropBase: vacuum after 2.5s, collect on contact
- UpgradeOption: Evolution|WeaponUpgrade|NewWeapon|Passive
- IWeightedRandom: weighted selection for drops/spawns
- UnlockCondition: SurviveTime|KillCount|HealAmount|ReachLevel|KillElites

## Systems
- ✅ Player: WASD+Rigidbody2D, stats (HP/EXP/level/multipliers), 6 weapon slots, TotalHealed/EliteKillCount tracking
- ✅ Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive
- ✅ Enemies: chase AI, spawner time-scaling, elite 5min (count ramps per wave), mage
- ✅ Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive
- ✅ Drops: EXP/Gold/Health, vacuum mechanic, DropManager
- ✅ UI: HUD (HP/EXP/timer/weapon bar), result screen, UpgradeUI
- ✅ Difficulty: DifficultyManager drives HP/Speed/Damage/SpawnInterval multipliers over time
- ✅ Unlock: UnlockCondition struct on CharacterData, runtime IsUnlocked() check

## Perf Budget
- Max 500 enemies on-screen | 6 weapon limit | Weapon max level 8
- Pool: enemies, projectiles, drops (all prewarmed)
- Event-driven UI | Cached refs | Reusable lists | Min GC alloc
- SpatialGrid: O(1) proximity queries, cell size 8f

## State
- Phase 1: 48/48 ✅ | Phase 2: 45/45 ✅ | Phase 3-4: 0/52

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
