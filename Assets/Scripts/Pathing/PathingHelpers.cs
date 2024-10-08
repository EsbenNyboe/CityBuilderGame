using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class PathingHelpers
{
    private static readonly List<int2> PositionList = new();
    private static readonly List<int> SimplePositionsX = new();
    private static readonly List<int> SimplePositionsY = new();
    private static readonly List<int> NeighbourDeltasX = new() { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly List<int> NeighbourDeltasY = new() { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly List<int> RandomNeighbourIndexList = new();
    private static readonly List<int> RandomNearbyCellIndexList = new();

    private static int2[] PositionListWith30Rings;

    public static void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, GridSetup.Instance.PathGrid.GetWidth() - 1);
        y = math.clamp(y, 0, GridSetup.Instance.PathGrid.GetHeight() - 1);
    }

    public static bool IsPositionInsideGrid(int2 cell)
    {
        return
            cell.x >= 0 &&
            cell.y >= 0 &&
            cell.x < GridSetup.Instance.PathGrid.GetWidth() &&
            cell.y < GridSetup.Instance.PathGrid.GetHeight();
    }

    public static bool IsPositionInsideGrid(int x, int y)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < GridSetup.Instance.PathGrid.GetWidth() &&
            y < GridSetup.Instance.PathGrid.GetHeight();
    }

    public static bool IsPositionWalkable(int2 cell)
    {
        return IsPositionWalkable(cell.x, cell.y);
    }

    public static bool IsPositionWalkable(int x, int y)
    {
        return GridSetup.Instance.PathGrid.GetGridObject(x, y).IsWalkable();
    }

    public static bool IsPositionOccupied(int2 cell)
    {
        return IsPositionOccupied(cell.x, cell.y);
    }

    public static bool IsPositionOccupied(int x, int y)
    {
        return GridSetup.Instance.OccupationGrid.GetGridObject(x, y).IsOccupied();
    }

    public static bool IsPositionDamageable(int2 cell)
    {
        return IsPositionDamageable(cell.x, cell.y);
    }

    public static bool IsPositionDamageable(int x, int y)
    {
        return GridSetup.Instance.DamageableGrid.GetGridObject(x, y).IsDamageable();
    }

    public static List<int2> GetCellListAroundTargetCell(int2 firstPosition, int ringCount)
    {
        PositionList.Clear();
        PositionList.Add(firstPosition);

        for (var i = 1; i < ringCount; i++)
        {
            for (var j = 1; j < i; j++)
            {
                AddFourPositionsAroundTarget(PositionList, firstPosition, i, j);
                AddFourPositionsAroundTarget(PositionList, firstPosition, j, i);
            }

            if (i - 1 > 0)
            {
                AddFourPositionsAroundTarget(PositionList, firstPosition, i - 1, i - 1);
            }

            PositionList.Add(firstPosition + new int2(i, 0));
            PositionList.Add(firstPosition + new int2(-i, 0));
            PositionList.Add(firstPosition + new int2(0, i));
            PositionList.Add(firstPosition + new int2(0, -i));
        }

        return PositionList;
    }

    private static void AddFourPositionsAroundTarget(List<int2> positionList, int2 firstPosition, int a, int b)
    {
        // if (positionList.Contains(firstPosition + new int2(a, b)))
        // {
        //     for (var i = 0; i < positionList.Count; i++)
        //     {
        //         if (positionList[i].Equals(firstPosition + new int2(a, b)))
        //         {
        //             Debug.Log("DUPLICATE IS THIS: " + i);
        //         }
        //     }
        //
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(-a, -b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(-a, b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(a, -b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }

        positionList.Add(firstPosition + new int2(a, b));
        positionList.Add(firstPosition + new int2(-a, -b));
        positionList.Add(firstPosition + new int2(-a, b));
        positionList.Add(firstPosition + new int2(a, -b));
    }

    public static void GetNeighbourCell(int index, int x, int y, out int neighbourX, out int neighbourY)
    {
        Assert.IsTrue(index >= 0 && index < 8, "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

        neighbourX = x + NeighbourDeltasX[index];
        neighbourY = y + NeighbourDeltasY[index];
    }

    public static (List<int>, List<int>) GetCellListAroundTargetCell(int targetX, int targetY, int ringCount, bool includeTarget = true)
    {
        SimplePositionsX.Clear();
        SimplePositionsY.Clear();

        // include the target-cell
        var nextCellX = targetX;
        var nextCellY = targetY;
        if (includeTarget)
        {
            AddPosition(nextCellX, nextCellY);
        }

        for (var ringSize = 1; ringSize < ringCount; ringSize++)
        {
            // start at max X & max Y
            nextCellX = targetX + ringSize;
            nextCellY = targetY + ringSize;

            // go to min X
            while (nextCellX > targetX - ringSize)
            {
                nextCellX--;
                AddPosition(nextCellX, nextCellY);
            }

            // go to min Y
            while (nextCellY > targetY - ringSize)
            {
                nextCellY--;
                AddPosition(nextCellX, nextCellY);
            }

            // go to max X
            while (nextCellX < targetX + ringSize)
            {
                nextCellX++;
                AddPosition(nextCellX, nextCellY);
            }

            // go to max Y
            while (nextCellY < targetY + ringSize)
            {
                nextCellY++;
                AddPosition(nextCellX, nextCellY);
            }
        }

        return (SimplePositionsX, SimplePositionsY);
    }

    private static void AddPosition(int nextCellX, int nextCellY)
    {
        SimplePositionsX.Add(nextCellX);
        SimplePositionsY.Add(nextCellY);
        // Debug.Log("New position: x: " + nextCellX + " y: " + nextCellY);
    }

    public static bool TryGetNearbyChoppingCell(int2 currentTarget, out int2 newTarget, out int2 newPathTarget)
    {
        var nearbyCells = GetCellListAroundTargetCell30Rings(currentTarget.x, currentTarget.y);

        if (GridSetup.Instance.DamageableGrid.GetGridObject(currentTarget.x, currentTarget.y).IsDamageable() &&
            TryGetValidNeighbourCell(currentTarget.x, currentTarget.y, out var newPathTargetX, out var newPathTargetY))
        {
            newTarget = currentTarget;
            newPathTarget = new int2(newPathTargetX, newPathTargetY);
            return true;
        }

        var count = nearbyCells.Length;

        var randomSelectionThreshold = math.min(50, count);
        InitializeRandomNearbyCellIndexList(0, randomSelectionThreshold);

        for (var i = 0; i < count; i++)
        {
            var cellIndex = i;
            if (!RandomNearbyCellIndexListIsEmpty())
            {
                cellIndex = GetRandomNearbyCellIndex();
            }

            var x = nearbyCells[cellIndex].x;
            var y = nearbyCells[cellIndex].y;

            if (!IsPositionInsideGrid(x, y) ||
                !GridSetup.Instance.DamageableGrid.GetGridObject(x, y).IsDamageable())
            {
                continue;
            }

            if (TryGetValidNeighbourCell(x, y, out newPathTargetX, out newPathTargetY))
            {
                newTarget = new int2(x, y);
                newPathTarget = new int2(newPathTargetX, newPathTargetY);
                return true;
            }
        }

        newTarget = default;
        newPathTarget = default;
        return false;
    }

    private static bool TryGetValidNeighbourCell(int x, int y, out int neighbourX, out int neighbourY)
    {
        InitializeRandomNeighbourIndexList(8);
        for (var j = 0; j < 8; j++)
        {
            var randomIndex = GetRandomNeighbourIndex();
            GetNeighbourCell(randomIndex, x, y, out neighbourX, out neighbourY);

            if (IsPositionInsideGrid(neighbourX, neighbourY) &&
                IsPositionWalkable(neighbourX, neighbourY) &&
                !IsPositionOccupied(neighbourX, neighbourY))
            {
                return true;
            }
        }

        neighbourX = -1;
        neighbourY = -1;
        return false;
    }

    private static void InitializeRandomNeighbourIndexList(int length)
    {
        RandomNeighbourIndexList.Clear();
        for (var i = 0; i < length; i++)
        {
            RandomNeighbourIndexList.Add(i);
        }
    }

    private static int GetRandomNeighbourIndex()
    {
        var indexListIndex = Random.Range(0, RandomNeighbourIndexList.Count);
        var cellListIndex = RandomNeighbourIndexList[indexListIndex];
        RandomNeighbourIndexList.RemoveAt(indexListIndex);
        return cellListIndex;
    }

    private static bool RandomNearbyCellIndexListIsEmpty()
    {
        return RandomNearbyCellIndexList.Count <= 0;
    }

    private static void InitializeRandomNearbyCellIndexList(int min, int max)
    {
        RandomNearbyCellIndexList.Clear();
        for (var i = min; i < max; i++)
        {
            RandomNearbyCellIndexList.Add(i);
        }
    }

    private static int GetRandomNearbyCellIndex()
    {
        var indexListIndex = Random.Range(0, RandomNearbyCellIndexList.Count);
        var cellListIndex = RandomNearbyCellIndexList[indexListIndex];
        RandomNearbyCellIndexList.RemoveAt(indexListIndex);
        return cellListIndex;
    }


    public static int2[] GetCellListAroundTargetCell30Rings(int targetX, int targetY)
    {
        return GetCellListAroundTargetCellPerformant(targetX, targetY, 30, ref PositionListWith30Rings);
    }

    private static int2[] GetCellListAroundTargetCellPerformant(int targetX, int targetY, int ringCount, ref int2[] cachedList)
    {
        if (cachedList == default)
        {
            cachedList = new int2[CalculatePositionListLength(ringCount)];
        }

        var index = 0;
        // include the target-cell
        cachedList[index].x = targetX;
        cachedList[index].y = targetY;

        for (var ringSize = 1; ringSize < ringCount; ringSize++)
        {
            // start at max X & max Y
            var x = targetX + ringSize;
            var y = targetY + ringSize;

            // go to min X
            while (x > targetX - ringSize)
            {
                index++;
                x--;
                cachedList[index].x = x;
                cachedList[index].y = y;
            }

            // go to min Y
            while (y > targetY - ringSize)
            {
                index++;
                y--;
                cachedList[index].x = x;
                cachedList[index].y = y;
            }

            // go to max X
            while (x < targetX + ringSize)
            {
                index++;
                x++;
                cachedList[index].x = x;
                cachedList[index].y = y;
            }

            // go to max Y
            while (y < targetY + ringSize)
            {
                index++;
                y++;
                cachedList[index].x = x;
                cachedList[index].y = y;
            }
        }

        return cachedList;
    }

    private static int CalculatePositionListLength(int ringCount)
    {
        var length = 1;
        for (var ringSize = 1; ringSize < ringCount; ringSize++)
        {
            // start at max X & max Y
            var x = ringSize;
            var y = ringSize;

            // go to min X
            while (x > -ringSize)
            {
                length++;
                x--;
            }

            // go to min Y
            while (y > -ringSize)
            {
                length++;
                y--;
            }

            // go to max X
            while (x < ringSize)
            {
                length++;
                x++;
            }

            // go to max Y
            while (y < ringSize)
            {
                length++;
                y++;
            }
        }

        return length;
    }
}