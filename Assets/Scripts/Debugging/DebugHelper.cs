using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

    public static void DebugDrawCell(int2 cell, Color color)
    {
        var padding = 0f;
        var offset = 1f - padding;
        var debugPosition = new Vector3(cell.x - 0.5f + padding, cell.y - 0.5f + padding, 0);
        Debug.DrawLine(debugPosition,
            debugPosition + new Vector3(+offset, +0), color);
        Debug.DrawLine(debugPosition,
            debugPosition + new Vector3(+0, +offset), color);
        Debug.DrawLine(debugPosition + new Vector3(+offset, +0),
            debugPosition + new Vector3(+offset, +offset), color);
        Debug.DrawLine(debugPosition + new Vector3(+0, +offset),
            debugPosition + new Vector3(+offset, +offset), color);
    }

    public static void DebugDrawCross(int2 cell, Color color)
    {
        var padding = 0.2f;
        var offset = 1f - padding;
        var debugPosition = new Vector3(cell.x - 0.5f + padding, cell.y - 0.5f + padding, 0);
        Debug.DrawLine(debugPosition, debugPosition + new Vector3(+offset, +offset), color);
        Debug.DrawLine(debugPosition + new Vector3(+offset, +0), debugPosition + new Vector3(0, +offset),
            color);
    }
}