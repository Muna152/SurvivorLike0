using UnityEngine;

public static class DebugLogger
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.Log($"[{timestamp}] {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogWarning($"[{timestamp}] {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        Debug.LogError($"[{timestamp}] {message}");
    }
}
