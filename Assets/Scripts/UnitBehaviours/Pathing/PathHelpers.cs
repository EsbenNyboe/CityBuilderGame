using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class PathHelpers
{
    public static bool TrySetPath(EntityCommandBuffer.ParallelWriter ecb, int index, Entity entity, int2 startCell,
        int2 endCell)
    {
        if (PathIsRedundant(startCell, endCell))
        {
            return false;
        }

        ecb.AddComponent(index, entity, new PathfindingParams
        {
            StartPosition = new int2(startCell.x, startCell.y),
            EndPosition = new int2(endCell.x, endCell.y)
        });
        return true;
    }

    public static bool TrySetPath(EntityCommandBuffer ecb, Entity entity, int2 startCell, int2 endCell)
    {
        if (PathIsRedundant(startCell, endCell))
        {
            return false;
        }

        ecb.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startCell.x, startCell.y),
            EndPosition = new int2(endCell.x, endCell.y)
        });
        return true;
    }

    private static bool PathIsRedundant(int2 startCell, int2 endCell)
    {
        if (startCell.x == endCell.x && startCell.y == endCell.y)
        {
            Debug.Log("No need to set a path. Already at destination");
            return true;
        }

        return false;
    }
}