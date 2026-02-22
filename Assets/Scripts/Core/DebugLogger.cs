using UnityEngine;

/// <summary>
/// Centralized logging utility that can be disabled in production builds
/// </summary>
public static class DebugLogger
{
    // Set to false in production builds
    private const bool EnableLogging = true;

    /// <summary>
    /// Logs a message to the console (only in development builds)
    /// </summary>
    public static void Log(object message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableLogging)
        {
            Debug.Log(message);
        }
        #endif
    }

    /// <summary>
    /// Logs a warning to the console
    /// </summary>
    public static void LogWarning(object message)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableLogging)
        {
            Debug.LogWarning(message);
        }
        #endif
    }

    /// <summary>
    /// Logs an error to the console (always logged)
    /// </summary>
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// Logs a formatted message to the console
    /// </summary>
    public static void LogFormat(string format, params object[] args)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableLogging)
        {
            Debug.LogFormat(format, args);
        }
        #endif
    }
}
