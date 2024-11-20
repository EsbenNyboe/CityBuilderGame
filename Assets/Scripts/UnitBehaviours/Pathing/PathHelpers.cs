using Unity.Entities;
using Unity.Mathematics;

public static class PathHelpers
{
    public static bool TrySetPath(EntityCommandBuffer.ParallelWriter ecb, int index, Entity entity,
        int2 startCell, int2 endCell, bool isDebugging = false)
    {
        if (PathIsRedundant(startCell, endCell, isDebugging))
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
        if (PathIsRedundant(startCell, endCell, isDebugging))
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

    private static bool PathIsRedundant(int2 startCell, int2 endCell, bool isDebugging)
    {
        if (startCell.x == endCell.x && startCell.y == endCell.y)
        {
            if (isDebugging)
            {
                DebugHelper.Log("No need to set a path. Already at destination");
            }

            return true;
        }

        return false;
    }
}