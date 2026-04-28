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

    /// <summary>Fire when the player dies.</summary>
    public static event Action OnPlayerDied;

    /// <summary>Fire when the player levels up. Parameter = new level.</summary>
    public static event Action<int> OnPlayerLevelUp;

    // ── Enemy Events ───────────────────────────────────────────
    /// <summary>Fire when an enemy dies. Parameter = the enemy that died.</summary>
    public static event Action<EnemyBase> OnEnemyDied;

    /// <summary>Fire when an enemy spawns. Parameter = the spawned enemy.</summary>
    public static event Action<EnemyBase> OnEnemySpawned;

    // ── Drop Events ────────────────────────────────────────────
    /// <summary>Fire when a drop item is collected. Parameter = the collected drop.</summary>
    public static event Action<DropBase> OnDropCollected;

    // ── Weapon Events ─────────────────────────────────────────
    /// <summary>Fire when a weapon evolves. Parameter = the evolved weapon.</summary>
    public static event Action<WeaponBase> OnWeaponEvolved;

    // ── Invoke Helpers ─────────────────────────────────────────
    // Centralised invoke points so we never miss null-checks.
    // Also makes it easy to add logging / analytics later.

    public static void InvokePlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
    public static void InvokePlayerDied() => OnPlayerDied?.Invoke();
    public static void InvokePlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
    public static void InvokeEnemyDied(EnemyBase enemy) => OnEnemyDied?.Invoke(enemy);
    public static void InvokeEnemySpawned(EnemyBase enemy) => OnEnemySpawned?.Invoke(enemy);
    public static void InvokeDropCollected(DropBase drop) => OnDropCollected?.Invoke(drop);
    public static void InvokeWeaponEvolved(WeaponBase weapon) => OnWeaponEvolved?.Invoke(weapon);

    /// <summary>
    /// Remove all subscribers. Useful when returning to the main menu or between runs.
    /// </summary>
    public static void ClearAll()
    {
        OnPlayerDamaged = null;
        OnPlayerDied = null;
        OnPlayerLevelUp = null;
        OnEnemyDied = null;
        OnEnemySpawned = null;
        OnDropCollected = null;
        OnWeaponEvolved = null;
    }
}