using UnityEngine;

/// <summary>
/// Generic MonoBehaviour singleton base class.
/// Provides global access via Instance, handles duplicate instances, and persists across scene loads.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (FindObjectsOfType<T>().Length > 1)
                    {
                        Debug.LogWarning($"[Singleton] Multiple instances of {typeof(T)} found. This should not happen.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        var singleton = new GameObject($"[Singleton] {typeof(T).Name}");
                        _instance = singleton.AddComponent<T>();
                        DontDestroyOnLoad(singleton);
                        Debug.Log($"[Singleton] Created instance of {typeof(T).Name} with DontDestroyOnLoad.");
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T).Name} destroyed on {gameObject.name}.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    public static bool HasInstance => _instance != null && !_applicationIsQuitting;
}