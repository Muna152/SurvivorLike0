# SurvivorLike0 ContextCard

## Engine
- Tuanjie 2023.2 | Built-in RP | .NET Standard 2.1 | 2D top-down

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
- Singleton<T>: GameManager, PoolManager, DropManager
- ObjectPool<T> w/ prewarm & HashSet tracking
- GameEvents static bus (OnEnemyDied, OnPlayerLevelUp, etc.)
- WeaponBase abstract → Projectile|Orbital|Area|Auxiliary
- Factory: WeaponData.WeaponType → component creation

## Interfaces
- WeaponBase: cooldown, auto-attack, level stats
- EnemyBase: chase, takeDamage, die→spawn drops
- DropBase: vacuum after 2.5s, collect on contact
- UpgradeOption: Evolution|WeaponUpgrade|NewWeapon|Passive
- IWeightedRandom: weighted selection for drops/spawns

## Systems
- ✅ Player: WASD+Rigidbody2D, stats (HP/EXP/level/multipliers), 6 weapon slots
- ✅ Weapons: 4 types + HolyLight/HolyWater/OrbitalObject, evolution w/ passive
- ✅ Enemies: chase AI, spawner time-scaling (1.5s→batch 3-15), elite 5min, mage
- ✅ Upgrade: level-up→3 options, priority: evolution>upgrade>new>passive
- ✅ Drops: EXP/Gold/Health, vacuum mechanic, DropManager
- ✅ UI: HUD (HP/EXP/timer/weapon bar), result screen, UpgradeUI

## Perf Budget
- Max 500 enemies on-screen | 6 weapon limit | Weapon max level 8
- Pool: enemies, projectiles, drops (all prewarmed)
- Event-driven UI | Cached refs | Reusable lists | Min GC alloc

## State
- Phase 1: 48/48 ✅ | Phase 2: 39/45 | Phase 3-4: 0/52
- T2.5.1~T2.5.4 SpatialGrid + perf: ✅ Done
- **AreaWeapon伤害判定 bug修复**: ✅ 完成
  - 根因1: `CellKey(0,0)=0` 与 "未注册"哨兵值冲突 → 已修复(Unregister幂等化)
  - 根因2: index-based stagger因_registered列表增删导致索引漂移 → 已移除stagger
  - 根因3: SpawnEliteWave对EliteEnemy调用Initialize两次 → 已修复
  - 根因4: _registered List与_registeredSet HashSet双集合不同步,UpdateAll迭代List漏掉敌人 → 已合并为单一HashSet _registered
  - 根因5: 查询时grid可能过期 → QueryInRadius前调用Reconcile()强制同步所有cell
  - **待验证**: ~~运行游戏确认 Missed:0~~ ✅ 已验证, 诊断日志已注释

## Next (Phase 2 remaining — 5 tasks)
- ✅ AreaWeapon伤害判定bug修复已验证完成, 诊断日志已注释
- T2.6.1 DifficultyManager → T2.6.2 enemy HP scaling → T2.6.3 spawn density curve → T2.6.4 elite wave timer → T2.6.5 character unlock

## Key Decisions
- Built-in RP: simpler 2D, fewer shader compat issues
- SO data-driven: designers tweak without recompile
- Static events: zero-coupling, any system can subscribe
- Tuanjie over Unity: project originated on this engine fork
- AreaWeapon split strategy: _followsPlayer auras refresh duration; puddles only create when none exists, expire naturally, respawn on next CD

## Known Issues
- PassiveData reference breakage: null-guards in place, root cause (duplicate dirs) fixed in 1326bbd
