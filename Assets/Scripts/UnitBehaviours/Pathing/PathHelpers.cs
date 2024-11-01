using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class PathHelpers
{
    public static bool TrySetPath(EntityCommandBuffer ecb, Entity entity, int2 startCell, int2 endCell)
    {
        return TrySetPath(ecb, entity, startCell.x, startCell.y, endCell.x, endCell.y);
    }

    public static bool TrySetPath(EntityCommandBuffer ecb, Entity entity, int startX, int startY, int endX, int endY)
    {
        if (startX == endX && startY == endY)
        {
            Debug.Log("No need to set a path. Already at destination");
            return false;
        }

        ecb.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = new int2(endX, endY)
        });
        return true;
    }
}