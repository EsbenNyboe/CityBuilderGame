using Unity.Entities;
using Unity.Mathematics;

public static class PathHelpers
{
    public static bool TrySetPath(EntityCommandBuffer.ParallelWriter ecb, int index, Entity entity,
        int2 startCell, int2 endCell, bool isDebugging = false)
    {
        if (PathIsInvalid(startCell, endCell, isDebugging))
        {
            return false;
        }

        ecb.AddComponent(index, entity, new Pathfinding
        {
            StartPosition = new int2(startCell.x, startCell.y),
            EndPosition = new int2(endCell.x, endCell.y)
        });
        return true;
    }

    public static bool TrySetPath(EntityCommandBuffer ecb, Entity entity,
        float3 startPosition, float3 endPosition, bool isDebugging = false)
    {
        var startCell = GridHelpers.GetXY(startPosition);
        var endCell = GridHelpers.GetXY(endPosition);
        return TrySetPath(ecb, entity, startCell, endCell, isDebugging);
    }

    public static bool TrySetPath(EntityCommandBuffer ecb, Entity entity,
        int2 startCell, int2 endCell, bool isDebugging = false)
    {
        if (PathIsInvalid(startCell, endCell, isDebugging))
        {
            return false;
        }

        ecb.AddComponent(entity, new Pathfinding
        {
            StartPosition = new int2(startCell.x, startCell.y),
            EndPosition = new int2(endCell.x, endCell.y)
        });
        return true;
    }

    private static bool PathIsInvalid(int2 startCell, int2 endCell, bool isDebugging)
    {
        if (isDebugging)
        {
            if (endCell.x < 0 || endCell.x >= GridManagerSystem.Width || endCell.y < 0 ||
                endCell.y > GridManagerSystem.Height)
            {
                DebugHelper.LogError("Path target is out of bounds!");
            }
        }

        if (isDebugging)
        {
            // Are there any downsides to allowing pathfinding, if you're already at your destination?
            // If you're not centered, I guess it makes sense to pathfind to the center of the cell, right?
            if (startCell.x == endCell.x && startCell.y == endCell.y)
            {
                DebugHelper.Log("No need to set a path. Already at destination");
            }
        }

        return false;
    }
}