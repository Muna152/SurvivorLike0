# Enemy Density Boost Design

**Date:** 2026-06-11
**Status:** Approved
**Author:** Codely CLI

## Overview

Enhance enemy spawn density and frequency to create more intense "monster waves" while maintaining elite wave balance. This adjustment targets players seeking higher difficulty and more chaotic combat.

## Problem Statement

Current enemy density feels too sparse for players desiring intense combat. The existing parameters (200 max enemies, 1.5s spawn interval, 3-15 batch size) result in manageable but underwhelming enemy waves.

## Solution

Adjust spawn parameters across GameBalanceConfig and EnemySpawner to significantly increase enemy density without modifying elite wave mechanics.

## Architecture

### Files Modified
- `Assets/Scripts/Data/GameBalanceConfig.cs` - Central balance configuration
- `Assets/Scripts/Enemies/EnemySpawner.cs` - Spawn timing and batch logic

### Parameter Changes

#### GameBalanceConfig.cs
```csharp
// Spawner section
maxEnemiesOnScreen: 200 → 500
baseBatchSize: 3 → 8
maxBatchSize: 15 → 30

// Elite wave section (UNCHANGED)
eliteWaveInterval: 300f (retained)
eliteWaveMinCount: 5 (retained)
eliteWaveMaxCount: 10 (retained)
eliteWaveCap: 20 (retained)
```

#### EnemySpawner.cs
```csharp
// Spawn timing
_baseSpawnInterval: 1.5f → 0.8f

// Boss spawn distance (UNCHANGED)
_bossSpawnDistance: 18f (retained)
```

## Data Flow

1. **Game Start**: EnemySpawner reads GameBalanceConfig values
2. **Spawn Loop**: Every 0.8s (vs 1.5s), spawn 8-30 enemies (vs 3-15)
3. **Cap Check**: Allow up to 500 active enemies (vs 200)
4. **Elite Waves**: Continue spawning every 5 minutes (unchanged)

## Performance Considerations

### CPU Impact
- **Enemy Count**: 2.5x increase (200→500) - SpatialGrid and SeparationPass scale O(n)
- **Spawn Rate**: 1.875x increase (1.5s→0.8s) - More frequent Update() calls
- **Batch Size**: 2x increase (15→30 max) - More simultaneous spawns

### Mitigation Strategies
- Existing SpatialGrid octree already optimizes enemy-to-enemy checks
- SeparationPass runs every 2 frames (not every frame) to reduce CPU load
- Far-LOD enemies skip separation calculations
- Object pooling (PoolManager) prevents GC pressure

### Recommended Monitoring
- Frame rate during high-density waves
- SpatialGrid performance metrics
- Unity Profiler for Physics.FixedUpdate and EnemyManager.FixedUpdate

## Testing Strategy

### Manual Testing
1. Play for 5+ minutes to observe density progression
2. Monitor frame rate during peak enemy count (~500)
3. Verify elite waves still spawn at 5-minute intervals
4. Check boss spawns (10/20/30 minutes) remain unaffected

### Automated Testing
- No new tests required (parameter-only changes)
- Existing EditMode tests validate config loading
- Existing PlayMode tests validate spawn logic

## Rollback Plan

If performance issues arise:
1. Reduce `maxEnemiesOnScreen` to 350 (intermediate value)
2. Increase `_baseSpawnInterval` to 1.0s (compromise between 0.8s and 1.5s)
3. Reduce `baseBatchSize` to 5 and `maxBatchSize` to 20 (moderate increase)

## Success Criteria

- Enemy density noticeably increased visually
- Combat feels more chaotic and intense
- Frame rate remains above 30 FPS during peak density
- Elite wave mechanics unchanged
- Boss spawn timing unchanged
- No new console errors or warnings

## Implementation Notes

- Changes are parameter-only; no logic modifications required
- DifficultyManager's SpawnIntervalMultiplier still applies (0.8s * multiplier)
- Time-based scaling (batchGrowthRate) continues to function
- No migration needed (ScriptableObject hot-reload)
