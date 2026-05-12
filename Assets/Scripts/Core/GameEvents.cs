using System;
using UnityEngine;

/// <summary>
/// Static event bus for game-wide communication.
/// Uses C# events to decouple systems. Subscribe/subscribe carefully to avoid memory leaks.
/// </summary>
public static class GameEvents
{
    // ── Player Events ──────────────────────────────────────────
    /// <summary>Fire when the player takes damage. Parameter = damage amount.</summary>
    public static event Action<int> OnPlayerDamaged;

    /// <summary>Fire when the player is healed. Parameter = heal amount.</summary>
    public static event Action<float> OnPlayerHealed;

    /// <summary>Fire when the player dies.</summary>
    public static event Action OnPlayerDied;

    /// <summary>Fire when the player levels up. Parameter = new level.</summary>
    public static event Action<int> OnPlayerLevelUp;

    // ── Enemy Events ───────────────────────────────────────────
    /// <summary>Fire when an enemy takes damage. Parameter = the enemy that was hit.</summary>
    public static event Action<EnemyBase> OnEnemyHit;

    /// <summary>Fire when an enemy takes damage. Parameters = the enemy + damage amount.</summary>
    public static event Action<EnemyBase, int> OnEnemyDamaged;

    /// <summary>Fire when an enemy dies. Parameter = the enemy that died.</summary>
    public static event Action<EnemyBase> OnEnemyDied;

    /// <summary>Fire when an enemy spawns. Parameter = the spawned enemy.</summary>
    public static event Action<EnemyBase> OnEnemySpawned;

    // ── Drop Events ────────────────────────────────────────────
    /// <summary>Fire when a drop item is collected. Parameter = the collected drop.</summary>
    public static event Action<DropBase> OnDropCollected;

    /// <summary>Fire when a chest is collected, triggering the chest-open sequence.</summary>
    public static event Action OnChestCollected;

    // ── Difficulty Events ─────────────────────────────────────
    /// <summary>Fire each game minute. Parameter = current minute (int).</summary>
    public static event Action<int> OnDifficultyChanged;

    // ── Weapon Events ─────────────────────────────────────────
    /// <summary>Fire when a weapon evolves. Parameter = the evolved weapon.</summary>
    public static event Action<WeaponBase> OnWeaponEvolved;

    /// <summary>Fire when any weapon change occurs (equip/upgrade/evolve/remove).</summary>
    public static event Action OnWeaponChanged;

    // ── Boss Events ────────────────────────────────────────────
    /// <summary>Fire when a boss spawns. Parameter = the boss that spawned.</summary>
    public static event Action<BossEnemy> OnBossSpawned;

    /// <summary>Fire when a boss dies. Parameter = the boss that died.</summary>
    public static event Action<BossEnemy> OnBossDied;

    /// <summary>Fire when a boss takes damage (for health bar). Parameter = the boss.</summary>
    public static event Action<BossEnemy> OnBossHealthChanged;

    // ── Game State Events ──────────────────────────────────────
    /// <summary>Fire when the game is paused.</summary>
    public static event Action OnGamePaused;

    /// <summary>Fire when the game is resumed from pause.</summary>
    public static event Action OnGameResumed;

    // ── Unlock Events ──────────────────────────────────────────
    /// <summary>Fire when a character is unlocked. Parameter = character id.</summary>
    public static event Action<string> OnCharacterUnlocked;

    // ── Meta-Progression Events ────────────────────────────────
    /// <summary>Fire when the player picks up gold in-game. Parameter = current session gold.</summary>
    public static event Action<int> OnGoldChanged;

    /// <summary>Fire when a permanent upgrade is purchased in the shop.</summary>
    public static event Action OnPermanentUpgradePurchased;

    // ── Invoke Helpers ─────────────────────────────────────────
    // Centralised invoke points so we never miss null-checks.
    // Also makes it easy to add logging / analytics later.

    public static void InvokePlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
    public static void InvokePlayerHealed(float amount) => OnPlayerHealed?.Invoke(amount);
    public static void InvokePlayerDied() => OnPlayerDied?.Invoke();
    public static void InvokePlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
    public static void InvokeEnemyHit(EnemyBase enemy) => OnEnemyHit?.Invoke(enemy);
    public static void InvokeEnemyDamaged(EnemyBase enemy, int damage) => OnEnemyDamaged?.Invoke(enemy, damage);
    public static void InvokeEnemyDied(EnemyBase enemy) => OnEnemyDied?.Invoke(enemy);
    public static void InvokeEnemySpawned(EnemyBase enemy) => OnEnemySpawned?.Invoke(enemy);
    public static void InvokeDropCollected(DropBase drop) => OnDropCollected?.Invoke(drop);
    public static void InvokeChestCollected() => OnChestCollected?.Invoke();
    public static void InvokeDifficultyChanged(int minute) => OnDifficultyChanged?.Invoke(minute);
    public static void InvokeWeaponEvolved(WeaponBase weapon) => OnWeaponEvolved?.Invoke(weapon);
    public static void InvokeWeaponChanged() => OnWeaponChanged?.Invoke();
    public static void InvokeBossSpawned(BossEnemy boss) => OnBossSpawned?.Invoke(boss);
    public static void InvokeBossDied(BossEnemy boss) => OnBossDied?.Invoke(boss);
    public static void InvokeBossHealthChanged(BossEnemy boss) => OnBossHealthChanged?.Invoke(boss);
    public static void InvokeCharacterUnlocked(string characterId) => OnCharacterUnlocked?.Invoke(characterId);
    public static void InvokeOnGoldChanged(int currentGold) => OnGoldChanged?.Invoke(currentGold);
    public static void InvokeOnPermanentUpgradePurchased() => OnPermanentUpgradePurchased?.Invoke();
    public static void InvokeGamePaused() => OnGamePaused?.Invoke();
    public static void InvokeGameResumed() => OnGameResumed?.Invoke();

    /// <summary>
    /// Remove all subscribers. Useful when returning to the main menu or between runs.
    /// </summary>
    public static void ClearAll()
    {
        OnPlayerDamaged = null;
        OnPlayerHealed = null;
        OnPlayerDied = null;
        OnPlayerLevelUp = null;
        OnEnemyHit = null;
        OnEnemyDamaged = null;
        OnEnemyDied = null;
        OnEnemySpawned = null;
        OnDropCollected = null;
        OnChestCollected = null;
        OnDifficultyChanged = null;
        OnWeaponEvolved = null;
        OnWeaponChanged = null;
        OnBossSpawned = null;
        OnBossDied = null;
        OnBossHealthChanged = null;
        OnCharacterUnlocked = null;
        OnGoldChanged = null;
        OnPermanentUpgradePurchased = null;
        OnGamePaused = null;
        OnGameResumed = null;
    }
}