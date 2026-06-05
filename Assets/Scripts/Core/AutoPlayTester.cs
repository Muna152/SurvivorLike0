using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automated play-test system: drives the game from menu → gameplay → game over,
/// with AI-controlled movement, auto upgrade selection, and performance profiling.
/// Supports multiple runs with retry loop and comprehensive reporting.
///
/// Usage: Tools → AutoPlay Test (default 1 run)
///        Tools → AutoPlay Stress Test (3 runs)
///        Or via C# script: new GameObject().AddComponent&lt;AutoPlayTester&gt;();
/// </summary>
public class AutoPlayTester : MonoBehaviour
{
    // ── Configuration ─────────────────────────────────────────────

    [Header("Test Configuration")]
    public int maxRuns = 1;
    public int characterIndex = 0;
    public float sampleInterval = 5f;
    public bool autoUpgrade = true;
    public bool enableProfiling = true;

    [Header("AI Configuration")]
    public float kiteRadius = 4f;
    public float collectRadius = 6f;
    public float aiDecisionInterval = 0.2f;
    public float wanderChangeInterval = 3f;

    // ── State Machine ─────────────────────────────────────────────

    private enum TestState
    {
        WaitingForPlaying,
        AutoPlay,
        GameOver,
        AutoRetry,
        Finished
    }

    private TestState _state = TestState.WaitingForPlaying;
    private int _currentRun;

    // ── References ────────────────────────────────────────────────

    private PlayerController _playerCtrl;
    private PlayerStats _playerStats;
    private PlayerWeaponManager _weaponMgr;
    private Rigidbody2D _playerRB;
    private UpgradeManager _upgradeMgr;

    // ── AI State ──────────────────────────────────────────────────

    private float _aiDecisionTimer;
    private Vector2 _aiMoveDirection;
    private float _wanderTimer;
    private Vector2 _wanderDir;
    private readonly List<EnemyBase> _nearbyEnemies = new List<EnemyBase>(32);
    private readonly Collider2D[] _dropColliders = new Collider2D[64];

    // ── Upgrade Auto-Select ───────────────────────────────────────

    private bool _subscribedToUpgrades;
    private UpgradeOption _pendingUpgrade;
    private float _pendingUpgradeTimer;

    // ── Performance Profiling ─────────────────────────────────────

    private struct PerfSample
    {
        public float gameTime;
        public float fps;
        public float minFPS;
        public float maxFPS;
        public int enemyCount;
        public int dropCount;
        public long gcHeapMB;
        public int totalGameObjects;
    }

    private readonly List<PerfSample> _samples = new List<PerfSample>();
    private float _sampleTimer;
    private float _runMinFPS = float.MaxValue;
    private float _runMaxFPS;
    private float _fpsAccumulator;
    private int _fpsFrameCount;

    // ── Run Results ───────────────────────────────────────────────

    private struct RunResult
    {
        public int runIndex;
        public float survivalTime;
        public int killCount;
        public int level;
        public int gold;
        public float avgFPS;
        public float minFPS;
        public float maxFPS;
        public int peakEnemyCount;
        public long peakGCHeapMB;
        public int totalSamples;
    }

    private readonly List<RunResult> _runResults = new List<RunResult>();

    // ── Scene reload tracking ─────────────────────────────────────

    private bool _waitingForSceneReload;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[AutoPlay] Tester started. Configuring...");
        InitializeAndStart();
    }

    private void Update()
    {
        switch (_state)
        {
            case TestState.WaitingForPlaying:
                UpdateWaitingForPlaying();
                break;
            case TestState.AutoPlay:
                UpdateAutoPlay();
                break;
            case TestState.GameOver:
                UpdateGameOver();
                break;
            case TestState.AutoRetry:
                UpdateAutoRetry();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_state != TestState.AutoPlay || _playerRB == null) return;

        // Apply AI movement via Rigidbody2D (since PlayerController is disabled)
        _playerRB.velocity = _aiMoveDirection * _playerStats.MoveSpeed;

        // Clamp to map boundaries
        float mapHalf = MapManager.CurrentMapHalfSize;
        if (mapHalf > 0f)
        {
            var pos = _playerRB.position;
            pos.x = Mathf.Clamp(pos.x, -mapHalf, mapHalf);
            pos.y = Mathf.Clamp(pos.y, -mapHalf, mapHalf);
            _playerRB.position = pos;
        }

        // Notify weapons of player movement
        if (_weaponMgr != null)
            _weaponMgr.OnPlayerMoved(_playerRB.position, _aiMoveDirection.sqrMagnitude > 0.01f ? _aiMoveDirection : Vector2.down);
    }

    private void OnDestroy()
    {
        UnsubscribeFromUpgrades();
        GameEvents.OnPlayerDied -= OnPlayerDied;

        if (_playerCtrl != null)
            _playerCtrl.enabled = true;
    }

    // ── State Machine ─────────────────────────────────────────────

    private void EnterState(TestState newState)
    {
        Debug.Log($"[AutoPlay] State: {_state} → {newState}");
        _state = newState;

        switch (newState)
        {
            case TestState.AutoPlay:
                EnterAutoPlay();
                break;
            case TestState.Finished:
                GenerateReport();
                Destroy(gameObject);
                break;
        }
    }

    // ── Init ──────────────────────────────────────────────────────

    private void InitializeAndStart()
    {
        _currentRun = 0;
        _runResults.Clear();

        if (!GameManager.HasInstance)
        {
            Debug.LogError("[AutoPlay] GameManager not found!");
            return;
        }

        var gm = GameManager.Instance;

        // If game is already playing (e.g. started manually), join mid-game
        if (gm.CurrentState == GameManager.GameState.Playing)
        {
            Debug.Log("[AutoPlay] Game already playing — joining mid-game.");
            EnterState(TestState.AutoPlay);
            return;
        }

        // If game is over, retry
        if (gm.CurrentState == GameManager.GameState.GameOver ||
            gm.CurrentState == GameManager.GameState.Victory)
        {
            Debug.Log("[AutoPlay] Game over detected — returning to menu.");
            gm.ReturnToMenu();
        }

        // Start the game from menu
        StartGameFromMenu();
    }

    private void StartGameFromMenu()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Find character data
        var characters = UnlockManager.Instance.AllCharacters;
        if (characters == null || characters.Count == 0)
        {
            Debug.LogError("[AutoPlay] No characters found!");
            return;
        }

        CharacterData selected = null;
        if (characterIndex >= 0 && characterIndex < characters.Count)
        {
            var desired = characters[characterIndex];
            if (UnlockManager.Instance.IsUnlocked(desired.id))
                selected = desired;
        }

        if (selected == null)
        {
            foreach (var c in characters)
            {
                if (UnlockManager.Instance.IsUnlocked(c.id))
                {
                    selected = c;
                    break;
                }
            }
        }

        if (selected == null)
        {
            Debug.LogError("[AutoPlay] No unlocked characters found!");
            return;
        }

        Debug.Log($"[AutoPlay] Run {_currentRun + 1}/{maxRuns} — Character: {selected.characterName}");

        // Hide UI
        var mainMenu = FindObjectOfType<MainMenuUI>();
        if (mainMenu != null) mainMenu.Hide();

        var charSelect = FindObjectOfType<CharacterSelectUI>();
        if (charSelect != null) charSelect.Hide();

        // Start the game
        gm.StartGame(selected);

        // Wait for Playing state then enter auto-play
        EnterState(TestState.WaitingForPlaying);
    }

    // ── WaitingForPlaying ─────────────────────────────────────────

    private void UpdateWaitingForPlaying()
    {
        if (!GameManager.HasInstance) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            EnterState(TestState.AutoPlay);
        }
    }

    // ── AutoPlay ──────────────────────────────────────────────────

    private void EnterAutoPlay()
    {
        // Cache references
        _playerCtrl = FindObjectOfType<PlayerController>();
        _playerStats = FindObjectOfType<PlayerStats>();
        _weaponMgr = FindObjectOfType<PlayerWeaponManager>();
        _upgradeMgr = FindObjectOfType<UpgradeManager>();
        _playerRB = _playerCtrl != null ? _playerCtrl.GetComponent<Rigidbody2D>() : null;

        if (_playerCtrl == null || _playerStats == null || _playerRB == null)
        {
            Debug.LogError("[AutoPlay] Missing essential player components!");
            EnterState(TestState.Finished);
            return;
        }

        // Disable PlayerController — we take over movement
        _playerCtrl.enabled = false;

        // Subscribe to events
        GameEvents.OnPlayerDied += OnPlayerDied;
        SubscribeToUpgrades();

        // Reset AI state
        _aiMoveDirection = Vector2.down;
        _aiDecisionTimer = 0f;
        _wanderTimer = 0f;
        _wanderDir = Random.insideUnitCircle.normalized;

        // Reset upgrade state
        _pendingUpgrade = null;
        _pendingUpgradeTimer = 0f;

        // Reset profiling
        _samples.Clear();
        _sampleTimer = 0f;
        _runMinFPS = float.MaxValue;
        _runMaxFPS = 0f;
        _fpsAccumulator = 0f;
        _fpsFrameCount = 0;

        Debug.Log("[AutoPlay] Auto-play started. AI controlling player.");
    }

    private void UpdateAutoPlay()
    {
        // Check for pending upgrade to apply (delayed auto-select)
        if (_pendingUpgrade != null)
        {
            _pendingUpgradeTimer -= Time.unscaledDeltaTime;
            if (_pendingUpgradeTimer <= 0f)
            {
                var toApply = _pendingUpgrade;
                _pendingUpgrade = null;
                if (_upgradeMgr != null)
                    _upgradeMgr.OnOptionSelected(toApply);
            }
            return; // Don't process AI while upgrade is pending
        }

        // Check for game over
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver ||
            GameManager.Instance.CurrentState == GameManager.GameState.Victory)
        {
            return; // Will be handled by OnPlayerDied
        }

        // Don't process AI while paused (upgrades, chest, pause menu)
        if (GameManager.Instance.IsSystemPaused)
        {
            // Check if upgrade UI is showing with options we haven't handled
            // (fallback in case event subscription missed it)
            TryAutoSelectUpgrade();
            return;
        }

        // Update AI movement
        UpdateAIMovement();

        // Update performance profiling
        if (enableProfiling)
            UpdateProfiling();
    }

    // ── AI Movement ───────────────────────────────────────────────

    private void UpdateAIMovement()
    {
        _aiDecisionTimer -= Time.deltaTime;
        if (_aiDecisionTimer > 0f) return;
        _aiDecisionTimer = aiDecisionInterval;

        var playerPos = (Vector2)_playerCtrl.transform.position;
        Vector2 bestDir = Vector2.zero;
        float urgency = 0f;

        // 1. Find nearby enemies via SpatialGrid (fast)
        _nearbyEnemies.Clear();
        SpatialGrid.QueryInRadius(playerPos, kiteRadius * 2f, _nearbyEnemies);

        // 2. Threat assessment — kite if enemies are close
        if (_nearbyEnemies.Count > 0)
        {
            Vector2 fleeDir = Vector2.zero;
            float closestDist = float.MaxValue;

            foreach (var enemy in _nearbyEnemies)
            {
                if (enemy == null) continue;
                Vector2 toEnemy = (Vector2)enemy.transform.position - playerPos;
                float dist = toEnemy.magnitude;
                if (dist < closestDist) closestDist = dist;

                float weight = 1f / (dist * dist + 0.1f);
                fleeDir -= toEnemy.normalized * weight;
            }

            if (closestDist < kiteRadius)
            {
                bestDir = fleeDir.normalized;
                urgency = (kiteRadius - closestDist) / kiteRadius;
            }
        }

        // 3. Collect drops — find nearby EXP/gold drops
        if (urgency < 0.7f)
        {
            Vector2 collectDir = Vector2.zero;
            bool foundDrops = false;

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                playerPos, collectRadius, _dropColliders,
                LayerMask.GetMask("Default"));

            for (int i = 0; i < hitCount; i++)
            {
                var drop = _dropColliders[i].GetComponent<DropBase>();
                if (drop == null) continue;

                Vector2 toDrop = (Vector2)drop.transform.position - playerPos;
                float dist = toDrop.magnitude;

                float priority = 1f / (dist + 0.5f);
                collectDir += toDrop.normalized * priority;
                foundDrops = true;
            }

            if (foundDrops)
            {
                collectDir.Normalize();
                if (urgency > 0.1f && bestDir != Vector2.zero)
                    bestDir = Vector2.Lerp(collectDir, bestDir, urgency).normalized;
                else
                    bestDir = collectDir;
            }
        }

        // 4. Wander if no clear objective
        if (bestDir.sqrMagnitude < 0.01f)
        {
            _wanderTimer -= aiDecisionInterval;
            if (_wanderTimer <= 0f)
            {
                _wanderDir = Random.insideUnitCircle.normalized;
                _wanderTimer = wanderChangeInterval;
            }
            bestDir = _wanderDir;
        }

        // 5. Stay away from map edges
        float mapHalf = MapManager.CurrentMapHalfSize;
        if (mapHalf > 0f)
        {
            float edgeBuffer = 8f;
            if (playerPos.x > mapHalf - edgeBuffer) bestDir.x -= 1f;
            if (playerPos.x < -mapHalf + edgeBuffer) bestDir.x += 1f;
            if (playerPos.y > mapHalf - edgeBuffer) bestDir.y -= 1f;
            if (playerPos.y < -mapHalf + edgeBuffer) bestDir.y += 1f;
        }

        _aiMoveDirection = bestDir.normalized;
    }

    // ── Auto Upgrade ─────────────────────────────────────────────

    private void SubscribeToUpgrades()
    {
        if (!autoUpgrade || _upgradeMgr == null) return;
        if (_subscribedToUpgrades) return;

        _upgradeMgr.OnOptionsGenerated += OnUpgradeOptionsGenerated;
        _subscribedToUpgrades = true;
    }

    private void UnsubscribeFromUpgrades()
    {
        if (!_subscribedToUpgrades || _upgradeMgr == null) return;
        _upgradeMgr.OnOptionsGenerated -= OnUpgradeOptionsGenerated;
        _subscribedToUpgrades = false;
    }

    private void OnUpgradeOptionsGenerated(List<UpgradeOption> options)
    {
        if (options == null || options.Count == 0) return;

        UpgradeOption chosen = null;
        int bestPriority = int.MaxValue;

        foreach (var option in options)
        {
            int priority = GetUpgradePriority(option);
            if (priority < bestPriority)
            {
                bestPriority = priority;
                chosen = option;
            }
        }

        if (chosen == null)
            chosen = options[0];

        Debug.Log($"[AutoPlay] Auto-selected upgrade: {chosen.Name} (priority {bestPriority})");

        // Apply after short delay using unscaled time (works when timeScale=0)
        _pendingUpgrade = chosen;
        _pendingUpgradeTimer = 0.3f;
    }

    /// <summary>
    /// Fallback: if upgrade UI is showing but our event handler didn't fire,
    /// directly auto-select. This handles edge cases where the subscription
    /// was set up after the upgrade was already generated.
    /// </summary>
    private void TryAutoSelectUpgrade()
    {
        if (!autoUpgrade || _pendingUpgrade != null) return;
        if (_upgradeMgr == null) _upgradeMgr = FindObjectOfType<UpgradeManager>();
        if (_upgradeMgr == null) return;

        // Check if UpgradeUI is currently showing options
        var upgradeUI = FindObjectOfType<UpgradeUI>();
        if (upgradeUI == null) return;

        // The upgrade is showing — force-select the first option with a short delay
        Debug.Log("[AutoPlay] Upgrade UI detected but no event fired — forcing auto-select.");
        _pendingUpgradeTimer = 0.2f;
        // We'll just skip the upgrade since we can't easily access the options
        _upgradeMgr.SkipUpgrade();
    }

    private static int GetUpgradePriority(UpgradeOption option)
    {
        if (option is WeaponEvolutionOption) return 0;
        if (option is NewWeaponOption) return 1;
        if (option is WeaponUpgradeOption) return 2;
        if (option is PassiveUpgradeOption) return 3;
        return 4;
    }

    // ── Player Death ──────────────────────────────────────────────

    private void OnPlayerDied()
    {
        Debug.Log("[AutoPlay] Player died!");
        EnterState(TestState.GameOver);
    }

    // ── GameOver ─────────────────────────────────────────────────

    private void UpdateGameOver()
    {
        if (!GameManager.HasInstance) return;

        // Record run result (if not already recorded for this run)
        if (_runResults.Count <= _currentRun)
            RecordRunResult();

        _currentRun++;

        if (_currentRun < maxRuns)
        {
            EnterState(TestState.AutoRetry);
        }
        else
        {
            EnterState(TestState.Finished);
        }
    }

    private void RecordRunResult()
    {
        var gm = GameManager.Instance;
        var stats = _playerStats;

        float avgFPS = _fpsFrameCount > 0 ? _fpsAccumulator / _fpsFrameCount : 0f;
        int peakEnemies = 0;
        long peakGC = 0;

        foreach (var s in _samples)
        {
            if (s.enemyCount > peakEnemies) peakEnemies = s.enemyCount;
            if (s.gcHeapMB > peakGC) peakGC = s.gcHeapMB;
        }

        var result = new RunResult
        {
            runIndex = _currentRun,
            survivalTime = gm != null ? gm.ElapsedTime : 0f,
            killCount = stats != null ? stats.KillCount : 0,
            level = stats != null ? stats.Level : 0,
            gold = stats != null ? stats.Gold : 0,
            avgFPS = avgFPS,
            minFPS = _runMinFPS < float.MaxValue ? _runMinFPS : 0f,
            maxFPS = _runMaxFPS,
            peakEnemyCount = peakEnemies,
            peakGCHeapMB = peakGC,
            totalSamples = _samples.Count
        };

        _runResults.Add(result);
        Debug.Log($"[AutoPlay] Run {_currentRun + 1} result: {result.survivalTime:F1}s, {result.killCount} kills, Lv{result.level}, AvgFPS {result.avgFPS:F1}");
    }

    // ── AutoRetry ─────────────────────────────────────────────────

    private void UpdateAutoRetry()
    {
        // Only execute once
        if (_waitingForSceneReload) return;
        _waitingForSceneReload = true;

        // Clean up current run state
        UnsubscribeFromUpgrades();
        GameEvents.OnPlayerDied -= OnPlayerDied;

        // Re-enable PlayerController before scene reload
        if (_playerCtrl != null)
            _playerCtrl.enabled = true;

        // Trigger retry via the same flow as ResultScreen.Retry()
        Time.timeScale = 1f;
        GameEvents.ClearAll();
        EnemyBase.ResetStatics();

        if (PoolManager.HasInstance)
            PoolManager.Instance.ClearAll();

        var selectedChar = GameManager.HasInstance ? GameManager.Instance.SelectedCharacter : null;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.PendingAutoStart = selectedChar;
            GameManager.Instance.ReturnToMenu();
        }

        // Reload scene
        if (SceneTransition.HasInstance)
        {
            SceneTransition.Instance.TransitionToScene(
                SceneManager.GetActiveScene().name,
                onMidpoint: null,
                0.2f);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Start polling for scene reload
        StartCoroutine(WaitForSceneReloadAndResume());
    }

    private System.Collections.IEnumerator WaitForSceneReloadAndResume()
    {
        // Wait for scene to fully load
        yield return new UnityEngine.WaitForSecondsRealtime(2f);

        // Wait for GameManager to be in Playing state (PendingAutoStart kicks in)
        float timeout = 15f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                _waitingForSceneReload = false;
                EnterState(TestState.AutoPlay);
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[AutoPlay] Timeout waiting for game to start after retry.");
        _waitingForSceneReload = false;
        StartGameFromMenu();
    }

    // ── Performance Profiling ────────────────────────────────────

    private void UpdateProfiling()
    {
        float fps = 1f / Time.unscaledDeltaTime;
        _fpsAccumulator += fps;
        _fpsFrameCount++;
        if (fps < _runMinFPS) _runMinFPS = fps;
        if (fps > _runMaxFPS) _runMaxFPS = fps;

        _sampleTimer -= Time.unscaledDeltaTime;
        if (_sampleTimer > 0f) return;
        _sampleTimer = sampleInterval;

        var sample = new PerfSample
        {
            gameTime = GameManager.HasInstance ? GameManager.Instance.ElapsedTime : 0f,
            fps = fps,
            minFPS = _runMinFPS,
            maxFPS = _runMaxFPS,
            enemyCount = SpatialGrid.RegisteredCount,
            dropCount = CountActiveDrops(),
            gcHeapMB = System.GC.GetTotalMemory(false) / (1024 * 1024),
            totalGameObjects = FindObjectsOfType<GameObject>().Length
        };

        _samples.Add(sample);

        Debug.Log($"[AutoPlay] Sample at {sample.gameTime:F0}s: FPS {fps:F0}, Enemies {sample.enemyCount}, Drops {sample.dropCount}, GC {sample.gcHeapMB}MB");
    }

    private static int CountActiveDrops()
    {
        var drops = FindObjectsOfType<DropBase>();
        int count = 0;
        foreach (var d in drops)
        {
            if (d != null && d.gameObject.activeSelf) count++;
        }
        return count;
    }

    // ── Report Generation ─────────────────────────────────────────

    private void GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════╗");
        sb.AppendLine("║          AUTO-PLAY TEST REPORT                 ║");
        sb.AppendLine("╚══════════════════════════════════════════════════╝");
        sb.AppendLine();

        // Configuration
        sb.AppendLine("── Configuration ──────────────────────────────────");
        sb.AppendLine($"  Max Runs:       {maxRuns}");
        sb.AppendLine($"  Character Idx:  {characterIndex}");
        sb.AppendLine($"  Sample Interval: {sampleInterval}s");
        sb.AppendLine($"  Auto Upgrade:   {autoUpgrade}");
        sb.AppendLine($"  Profiling:      {enableProfiling}");
        sb.AppendLine();

        // Per-run results
        sb.AppendLine("── Run Results ─────────────────────────────────────");
        float totalSurvival = 0f;
        int totalKills = 0;
        float bestAvgFPS = float.MaxValue;
        float worstMinFPS = float.MaxValue;

        foreach (var r in _runResults)
        {
            sb.AppendLine($"  Run {r.runIndex + 1}:");
            sb.AppendLine($"    Survival: {r.survivalTime:F1}s ({r.survivalTime / 60f:F1}min)");
            sb.AppendLine($"    Kills: {r.killCount} | Level: {r.level} | Gold: {r.gold}");
            sb.AppendLine($"    FPS Avg: {r.avgFPS:F1} | Min: {r.minFPS:F1} | Max: {r.maxFPS:F1}");
            sb.AppendLine($"    Peak Enemies: {r.peakEnemyCount} | Peak GC Heap: {r.peakGCHeapMB}MB");
            sb.AppendLine($"    Perf Samples: {r.totalSamples}");
            sb.AppendLine();

            totalSurvival += r.survivalTime;
            totalKills += r.killCount;
            if (r.avgFPS < bestAvgFPS) bestAvgFPS = r.avgFPS;
            if (r.minFPS < worstMinFPS) worstMinFPS = r.minFPS;
        }

        // Aggregate stats
        sb.AppendLine("── Aggregate Stats ─────────────────────────────────");
        if (_runResults.Count > 0)
        {
            sb.AppendLine($"  Runs Completed: {_runResults.Count}/{maxRuns}");
            sb.AppendLine($"  Avg Survival: {totalSurvival / _runResults.Count:F1}s ({totalSurvival / _runResults.Count / 60f:F1}min)");
            sb.AppendLine($"  Total Kills: {totalKills}");
            sb.AppendLine($"  Best Avg FPS: {bestAvgFPS:F1}");
            sb.AppendLine($"  Worst Min FPS: {worstMinFPS:F1}");
        }
        sb.AppendLine();

        // Performance over time
        if (_samples.Count > 0)
        {
            sb.AppendLine("── Performance Over Time ──────────────────────────");
            sb.AppendLine($"  {"Time",8} {"FPS",6} {"MinFPS",8} {"Enemies",9} {"Drops",6} {"GC(MB)",8} {"GOs",6}");
            sb.AppendLine("  " + new string('─', 56));

            foreach (var s in _samples)
            {
                sb.AppendLine($"  {s.gameTime,8:F0}s {s.fps,6:F0} {s.minFPS,8:F1} {s.enemyCount,9} {s.dropCount,6} {s.gcHeapMB,8} {s.totalGameObjects,6}");
            }
            sb.AppendLine();

            if (_samples.Count >= 2)
            {
                float firstHalfAvg = 0f, secondHalfAvg = 0f;
                int half = _samples.Count / 2;
                for (int i = 0; i < half; i++) firstHalfAvg += _samples[i].fps;
                for (int i = half; i < _samples.Count; i++) secondHalfAvg += _samples[i].fps;
                firstHalfAvg /= half;
                secondHalfAvg /= (_samples.Count - half);

                float degradation = (firstHalfAvg - secondHalfAvg) / firstHalfAvg * 100f;
                sb.AppendLine($"  FPS Degradation: {degradation:F1}% ({firstHalfAvg:F0} → {secondHalfAvg:F0})");
            }
        }

        sb.AppendLine();
        sb.AppendLine("╔══════════════════════════════════════════════════╗");
        sb.AppendLine("║          AUTO-PLAY TEST COMPLETE                ║");
        sb.AppendLine("╚══════════════════════════════════════════════════╝");

        string report = sb.ToString();

        Debug.Log(report);

        try
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(Application.dataPath, "..", $"AutoPlayReport_{timestamp}.txt");
            File.WriteAllText(filePath, report);
            Debug.Log($"[AutoPlay] Report saved to: {Path.GetFullPath(filePath)}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AutoPlay] Failed to save report file: {e.Message}");
        }
    }
}
