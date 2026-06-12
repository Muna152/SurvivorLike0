# Enemy Density Boost Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Increase enemy spawn density and frequency to create more intense "monster waves" while maintaining elite wave balance.

**Architecture:** Parameter-only changes to GameBalanceConfig (ScriptableObject) and EnemySpawner (MonoBehaviour). No logic modifications required. Changes are hot-reloadable via Unity's asset pipeline.

**Tech Stack:** Unity, C#, ScriptableObject configuration system

---

### Task 1: Update GameBalanceConfig spawn parameters

**Files:**
- Modify: `Assets/Scripts/Data/GameBalanceConfig.cs`

- [ ] **Step 1: Modify maxEnemiesOnScreen parameter**

Locate the Spawner section in GameBalanceConfig.cs and update the maxEnemiesOnScreen value:

```csharp
[Header("Spawner")]
[Tooltip("Minimum spawn distance from player")]
public float minSpawnDistance = 15f;
[Tooltip("Maximum spawn distance from player")]
public float maxSpawnDistance = 25f;
[Tooltip("Global enemy cap on screen")]
public int maxEnemiesOnScreen = 500;  // Changed from 200
[Tooltip("Elite wave interval (seconds)")]
public float eliteWaveInterval = 300f;
```

- [ ] **Step 2: Modify baseBatchSize parameter**

Update the base batch size in the Spawner section:

```csharp
[Tooltip("Base batch size formula: floor(baseBatch + batchGrowthRate * minutes)")]
public int baseBatchSize = 8;  // Changed from 3
```

- [ ] **Step 3: Modify maxBatchSize parameter**

Update the maximum batch size in the Spawner section:

```csharp
[Tooltip("Maximum batch size per wave")]
public int maxBatchSize = 30;  // Changed from 15
```

- [ ] **Step 4: Compile and validate**

Run: Unity compilation (automatic after save)
Expected: No compilation errors, no console warnings

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Data/GameBalanceConfig.cs
git commit -m "feat: increase enemy density - maxEnemiesOnScreen 200→500, baseBatchSize 3→8, maxBatchSize 15→30"
```

---

### Task 2: Update EnemySpawner spawn interval

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemySpawner.cs`

- [ ] **Step 1: Modify _baseSpawnInterval field**

Locate the _baseSpawnInterval field declaration at the top of EnemySpawner.cs:

```csharp
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float _baseSpawnInterval = 0.8f;  // Changed from 1.5f
    [SerializeField] private float _bossSpawnDistance = 18f;
```

- [ ] **Step 2: Compile and validate**

Run: Unity compilation (automatic after save)
Expected: No compilation errors, no console warnings

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Enemies/EnemySpawner.cs
git commit -m "feat: increase spawn rate - _baseSpawnInterval 1.5s→0.8s"
```

---

### Task 3: Verify parameter values in Unity Editor

**Files:**
- No file changes (verification task)

- [ ] **Step 1: Open GameBalanceConfig in Inspector**

Navigate to: `Resources/GameBalanceConfig` in Unity Project window
Action: Click on GameBalanceConfig asset to open Inspector

- [ ] **Step 2: Verify spawn parameter values**

In Inspector, check Spawner section values:
- maxEnemiesOnScreen should be **500** (not 200)
- baseBatchSize should be **8** (not 3)
- maxBatchSize should be **30** (not 15)

- [ ] **Step 3: Verify elite wave parameters unchanged**

In Inspector, check Elite Enemy section:
- eliteWaveInterval should still be **300** (5 minutes)
- eliteWaveMinCount should still be **5**
- eliteWaveMaxCount should still be **10**
- eliteWaveCap should still be **20**

- [ ] **Step 4: Verify EnemySpawner values**

In Unity Hierarchy, locate EnemySpawner GameObject
Action: Select EnemySpawner to open Inspector
Verify: _baseSpawnInterval should be **0.8** (not 1.5)

- [ ] **Step 5: Take screenshot**

Use Unity screenshot: Game View showing current scene state
Save to: `screenshots/enemy-density-boost-verification.png`

- [ ] **Step 6: Commit verification**

```bash
git add screenshots/enemy-density-boost-verification.png
git commit -m "test: verify enemy density parameters in Unity Editor"
```

---

### Task 4: Manual play testing

**Files:**
- No file changes (testing task)

- [ ] **Step 1: Enter Play Mode**

In Unity Editor: Click Play button
Expected: Game starts normally, no console errors

- [ ] **Step 2: Observe enemy density for 5 minutes**

Watch gameplay and note:
- Enemy count increases significantly (should reach 200+ quickly)
- Spawn frequency is noticeably faster (waves every ~0.8s)
- Batch sizes are larger (8-30 enemies per wave)
- Combat feels more chaotic and intense

- [ ] **Step 3: Monitor frame rate during peak density**

Use Unity Profiler or FPS counter:
- Frame rate should remain above 30 FPS
- Note any performance dips during peak enemy count (~500)

- [ ] **Step 4: Verify elite wave timing**

Wait for 5 minutes (300 seconds):
- Elite wave should spawn at ~5:00 mark
- Elite wave count should be 5-10 (unchanged from before)
- Elite wave interval should still be 5 minutes

- [ ] **Step 5: Exit Play Mode**

In Unity Editor: Click Stop button
Expected: Clean exit, no console errors

- [ ] **Step 6: Document test results**

Create test notes file with observations:
```bash
cat > docs/superpowers/plans/enemy-density-test-results.md << 'EOF'
# Enemy Density Boost Test Results

**Test Date:** 2026-06-11
**Test Duration:** 5+ minutes

## Observations

### Enemy Density
- Peak enemy count: [actual number, expected ~500]
- Time to reach 200 enemies: [actual time, expected <2 minutes]
- Spawn interval: ~0.8s per wave ✓
- Batch size range: [actual range, expected 8-30]

### Performance
- Average FPS: [actual, expected >30]
- Lowest FPS: [actual during peak density]
- Frame rate dips: [describe if any]

### Elite Waves
- First elite wave at: [actual time, expected ~5:00]
- Elite wave count: [actual, expected 5-10]
- Elite wave mechanics: [unchanged ✓]

### Boss Spawns
- 10-minute boss: [spawned/not spawned]
- 20-minute boss: [spawned/not spawned]
- 30-minute boss: [spawned/not spawned]

## Issues Found
[List any issues or unexpected behavior]

## Overall Assessment
[PASS/FAIL - meets success criteria?]
EOF
git add docs/superpowers/plans/enemy-density-test-results.md
git commit -m "test: enemy density boost manual play test results"
```

---

### Task 5: Final verification and documentation

**Files:**
- No file changes (verification task)

- [ ] **Step 1: Check for console errors**

Open Unity Console window
Verify: No errors or warnings related to enemy spawning

- [ ] **Step 2: Verify design spec success criteria**

Check against design spec:
- ✓ Enemy density noticeably increased visually
- ✓ Combat feels more chaotic and intense
- ✓ Frame rate remains above 30 FPS during peak density
- ✓ Elite wave mechanics unchanged
- ✓ Boss spawn timing unchanged
- ✓ No new console errors or warnings

- [ ] **Step 3: Update design doc with completion status**

Edit: `docs/superpowers/specs/2026-06-11-enemy-density-boost-design.md`
Add section at end:
```markdown
## Implementation Status

**Status:** ✅ Completed 2026-06-11
**Test Results:** PASS - All success criteria met
**Performance:** Frame rate >30 FPS at peak density (500 enemies)
**Notes:** [Any final notes from testing]
```

- [ ] **Step 4: Final commit**

```bash
git add docs/superpowers/specs/2026-06-11-enemy-density-boost-design.md
git commit -m "docs: mark enemy density boost design as completed"
```

- [ ] **Step 5: Optional performance monitoring**

If performance issues were observed during testing:
- Consider reducing maxEnemiesOnScreen to 350 (intermediate value)
- Consider increasing _baseSpawnInterval to 1.0s (compromise)
- Consider reducing batch sizes (baseBatchSize=5, maxBatchSize=20)
- Apply changes as needed and re-test

---

## Rollback Plan

If issues arise during or after implementation:

### Quick Rollback (Parameter Reversion)
```bash
git revert HEAD~3  # Revert last 3 commits (Tasks 1, 2, and verification)
```

### Partial Rollback (Performance Issues)
If performance is problematic but density increase is desired:
- maxEnemiesOnScreen: 500 → 350
- _baseSpawnInterval: 0.8s → 1.0s
- baseBatchSize: 8 → 5
- maxBatchSize: 30 → 20

Apply these values in Unity Inspector directly (ScriptableObject changes are hot-reloadable).

## Success Criteria Checklist

- [ ] Enemy density noticeably increased visually
- [ ] Combat feels more chaotic and intense
- [ ] Frame rate remains above 30 FPS during peak density
- [ ] Elite wave mechanics unchanged
- [ ] Boss spawn timing unchanged
- [ ] No new console errors or warnings
- [ ] All tests pass
- [ ] Design documentation updated
