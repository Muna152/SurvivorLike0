using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool for Component types.
/// Uses HashSet for O(1) add/remove instead of List O(n).
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool;
    private readonly Func<T> _createFunc;
    private readonly Action<T> _resetAction;
    private readonly int _maxSize;
    private readonly HashSet<T> _active;  // O(1) add/remove

    /// <summary>Number of objects currently available in the pool.</summary>
    public int CountInactive => _pool.Count;

    /// <summary>Number of objects currently in use.</summary>
    public int CountActive => _active.Count;

    /// <summary>Total objects managed by this pool (active + inactive).</summary>
    public int CountAll => _active.Count + _pool.Count;

    public ObjectPool(Func<T> createFunc, Action<T> resetAction = null, int maxSize = 0)
    {
        _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        _resetAction = resetAction;
        _maxSize = maxSize > 0 ? maxSize : int.MaxValue;
        _pool = new Queue<T>();
        _active = new HashSet<T>();
    }

    /// <summary>
    /// Get an object from the pool. Creates a new one if empty.
    /// Skips destroyed (null) objects still lingering in the queue.
    /// </summary>
    public T Get()
    {
        T obj;

        while (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            if (obj != null) // Unity null-check also catches destroyed objects
            {
                obj.gameObject.SetActive(true);
                _active.Add(obj);
                return obj;
            }
        }

        obj = _createFunc();
        obj.gameObject.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    /// <summary>
    /// Return an object to the pool for reuse.
    /// Note: caller should NOT call ResetForReuse — the pool's resetAction handles it.
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        _active.Remove(obj);

        if (_pool.Count >= _maxSize)
        {
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
    /// Pre-warm the pool by creating instances upfront.
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
    /// Clear the pool: destroy all objects.
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

    /// <summary>
    /// Remove destroyed (Unity-null) objects from the active set.
    /// Called after scene loads to clean up references to objects destroyed by Unity.
    /// </summary>
    public void PurgeDestroyedFromActive()
    {
        _active.RemoveWhere(obj => obj == null);
    }
}
