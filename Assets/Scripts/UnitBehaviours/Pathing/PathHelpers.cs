using Unity.Entities;
using Unity.Mathematics;

public static class PathHelpers
{
    public static bool TrySetPath(EntityCommandBuffer.ParallelWriter ecb, int index, GridManager gridManager,
        Entity entity, int2 startCell, int2 endCell, bool isDebugging = false)
    {
        if (PathIsInvalid(gridManager, startCell, endCell, isDebugging))
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

    public static bool TrySetPath(EntityCommandBuffer ecb, GridManager gridManager,
        Entity entity, float3 startPosition, float3 endPosition, bool isDebugging = false)
    {
        var startCell = GridHelpers.GetXY(startPosition);
        var endCell = GridHelpers.GetXY(endPosition);
        return TrySetPath(ecb, gridManager, entity, startCell, endCell, isDebugging);
    }

    public static bool TrySetPath(EntityCommandBuffer ecb, GridManager gridManager,
        Entity entity, int2 startCell, int2 endCell, bool isDebugging = true)
    {
        if (PathIsInvalid(gridManager, startCell, endCell, isDebugging))
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

    private static bool PathIsInvalid(GridManager gridManager, int2 startCell, int2 endCell, bool isDebugging)
    {
        if (!gridManager.IsWalkable(startCell))
        {
            if (isDebugging)
            {
                DebugHelper.LogError("Path start is not walkable!!");
            }

            return true;
        }

        if (!gridManager.IsWalkable(endCell))
        {
            if (isDebugging)
            {
                DebugHelper.LogError("Path end is not walkable!!");
            }

            return true;
        }

        if (!gridManager.IsMatchingSection(startCell, endCell))
        {
            if (isDebugging)
            {
                DebugHelper.Log("Path spans multiple sections!");
            }

            return true;
        }

        if (endCell.x < 0 || endCell.x >= gridManager.Width ||
            endCell.y < 0 || endCell.y > gridManager.Height)
        {
            if (isDebugging)
            {
                DebugHelper.LogError("Path end is out of bounds!");
            }

            return true;
        }

        return false;
    }
}