using Unity.Entities;
using UnityEngine;

public static class BurstDebugHelpers
{
    public static void DebugLog(string message)
    {
        Debug.Log(message);
    }

    public static void DebugLogWarning(string message)
    {
        // Debug.LogWarning(message);
    }

    public static void DebugLogError(string message, Entity entity)
    {
        // Debug.LogError(message + entity);
    }

    public static void DebugLogError(string message)
    {
        // Debug.LogError(message);
    }
}