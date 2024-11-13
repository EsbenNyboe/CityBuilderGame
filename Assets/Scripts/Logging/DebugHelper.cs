using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class DebugHelper
{
    private const bool EnableDebugLog = true;

    public static void Log(string message)
    {
        if (EnableDebugLog)
        {
            Debug.Log(message);
        }
    }

    public static void Log(int intMessage)
    {
        if (EnableDebugLog)
        {
            Debug.Log(intMessage.ToString());
        }
    }

    public static void Log(string message, Entity entity)
    {
        if (EnableDebugLog)
        {
            Debug.Log(ConvertToFixedString(message, entity));
        }
    }

    public static void LogWarning(string message)
    {
        if (EnableDebugLog)
        {
            Debug.LogWarning(message);
        }
    }

    public static void LogWarning(string message, Entity entity)
    {
        if (EnableDebugLog)
        {
            Debug.Log(ConvertToFixedString(message, entity));
        }
    }

    public static void LogError(string message)
    {
        if (EnableDebugLog)
        {
            Debug.LogError(message);
        }
    }

    public static void LogError(string message, Entity entity)
    {
        if (EnableDebugLog)
        {
            Debug.LogError(ConvertToFixedString(message, entity));
        }
    }

    private static FixedString128Bytes ConvertToFixedString(string messageString, Entity entity)
    {
        FixedString128Bytes messageFixedString = messageString;
        messageFixedString.Append(entity.ToFixedString());
        return messageFixedString;
    }
}