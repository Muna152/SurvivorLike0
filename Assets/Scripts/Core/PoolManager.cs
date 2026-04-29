using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralised object pool manager. Manages multiple named ObjectPools.
/// Uses string keys (typically prefab names) to index pools.
/// </summary>
public class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<string, object> _pools = new Dictionary<string, object>();

    /// <summary>
    /// Register a new object pool.
    /// </summary>
    /// <param name="key">Unique key for this pool (usually the prefab name).</param>
    /// <param name="createFunc">Factory function that creates a new instance.</param>
    /// <param name="resetAction">Action to reset the object when returned to the pool.</param>
    /// <param name="prewarmCount">Number of instances to pre-create.</param>
    /// <param name="maxSize">Maximum pool capacity. 0 = unlimited.</param>
    public void Register<T>(string key, Func<T> createFunc, Action<T> resetAction = null, int prewarmCount = 0, int maxSize = 0) where T : Component
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[PoolManager] Cannot register pool with null or empty key.");
            return;
        }

        if (_pools.ContainsKey(key))
        {
            // Pool already registered – not an error, just skip
            return;
        }

        var pool = new ObjectPool<T>(createFunc, resetAction, maxSize);
        if (prewarmCount > 0)
        {
            pool.Prewarm(prewarmCount);
        }

        _pools[key] = pool;
    }

    /// <summary>
    /// Check if a pool with the given key exists.
    /// </summary>
    public bool HasPool(string key)
    {
        return _pools.ContainsKey(key);
    }

    /// <summary>
    /// Get an object from the named pool.
    /// </summary>
    public T Get<T>(string key) where T : Component
    {
        if (!_pools.TryGetValue(key, out var poolObj))
        {
            Debug.LogError($"[PoolManager] No pool registered with key '{key}'. Did you forget to call Register?");
            return null;
        }

        var pool = poolObj as ObjectPool<T>;
        if (pool == null)
        {
            Debug.LogError($"[PoolManager] Pool '{key}' exists but is not of type ObjectPool<{typeof(T).Name}>.");
            return null;
        }

        return pool.Get();
    }

    /// <summary>
    /// Return an object to its named pool.
    /// </summary>
    public void Return<T>(string key, T obj) where T : Component
    {
        if (obj == null) return;

        if (!_pools.TryGetValue(key, out var poolObj))
        {
            // Pool not found – just destroy the object
            Debug.LogWarning($"[PoolManager] No pool for key '{key}'. Destroying object instead.");
            Destroy(obj.gameObject);
            return;
        }

        var pool = poolObj as ObjectPool<T>;
        if (pool == null)
        {
            Debug.LogError($"[PoolManager] Pool '{key}' type mismatch. Cannot return {typeof(T).Name}.");
            Destroy(obj.gameObject);
            return;
        }

        pool.Return(obj);
    }

    /// <summary>
    /// Remove and clear a specific pool.
    /// </summary>
    public void Unregister(string key)
    {
        if (_pools.TryGetValue(key, out var poolObj))
        {
            _pools.Remove(key);
            // Clear via reflection is acceptable here (infrequent operation)
            var clearMethod = poolObj.GetType().GetMethod("Clear");
            clearMethod?.Invoke(poolObj, null);
        }
    }

    /// <summary>
    /// Clear all pools and remove all registrations.
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in _pools)
        {
            var clearMethod = kvp.Value.GetType().GetMethod("Clear");
            clearMethod?.Invoke(kvp.Value, null);
        }

        _pools.Clear();
    }
}