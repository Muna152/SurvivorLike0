using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool for Component types.
/// Replaces runtime Instantiate/Destroy with Get/Return to reduce GC and allocation overhead.
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool;
    private readonly Func<T> _createFunc;
    private readonly Action<T> _resetAction;
    private readonly int _maxSize;
    private readonly List<T> _active;   // tracks all spawned (in-use) objects

    /// <summary>Number of objects currently available in the pool.</summary>
    public int CountInactive => _pool.Count;

    /// <summary>Number of objects currently in use.</summary>
    public int CountActive => _active.Count;

    /// <summary>Total objects managed by this pool (active + inactive).</summary>
    public int CountAll => _active.Count + _pool.Count;

    /// <summary>
    /// Create a new object pool.
    /// </summary>
    /// <param name="createFunc">Factory function that creates a new instance of T.</param>
    /// <param name="resetAction">Action invoked when an object is returned to the pool (reset state).</param>
    /// <param name="maxSize">Maximum pool capacity. 0 = unlimited. When exceeded, returned objects are destroyed.</param>
    public ObjectPool(Func<T> createFunc, Action<T> resetAction = null, int maxSize = 0)
    {
        _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        _resetAction = resetAction;
        _maxSize = maxSize > 0 ? maxSize : int.MaxValue;
        _pool = new Queue<T>();
        _active = new List<T>();
    }

    /// <summary>
    /// Get an object from the pool. If the pool is empty, a new instance is created.
    /// </summary>
    public T Get()
    {
        T obj;

        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            obj = _createFunc();
        }

        obj.gameObject.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    /// <summary>
    /// Return an object to the pool for reuse.
    /// If the pool is at max capacity, the object is destroyed instead.
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        _active.Remove(obj);

        if (_pool.Count >= _maxSize)
        {
            // Pool full – destroy the object
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(obj.gameObject);
            return;
        }

        _resetAction?.Invoke(obj);
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// Pre-warm the pool by creating <paramref name="count"/> instances and adding them to the pool.
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = _createFunc();
            _resetAction?.Invoke(obj);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Clear the pool: destroy all inactive objects and release active ones.
    /// </summary>
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            if (obj != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(obj.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(obj.gameObject);
            }
        }

        foreach (var obj in _active)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(obj.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(obj.gameObject);
            }
        }

        _active.Clear();
    }
}