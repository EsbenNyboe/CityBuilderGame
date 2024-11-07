using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class DebugLogHelper
{
    public static void DebugLog(string message, Entity entity)
    {
        Debug.Log(ConvertToFixedString(message, entity));
    }

    public static void DebugLogWarning(string message, Entity entity)
    {
        Debug.Log(ConvertToFixedString(message, entity));
    }

    public static void DebugLogError(string message, Entity entity)
    {
        Debug.LogError(ConvertToFixedString(message, entity));
    }

    private static FixedString128Bytes ConvertToFixedString(string messageString, Entity entity)
    {
        FixedString128Bytes messageFixedString = messageString;
        messageFixedString.Append(entity.ToFixedString());
        return messageFixedString;
    }
}