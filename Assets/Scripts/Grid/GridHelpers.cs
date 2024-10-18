using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public static class GridHelpers
{
    private static readonly List<int2> PositionList = new();
    private static readonly List<int> NeighbourDeltasX = new() { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly List<int> NeighbourDeltasY = new() { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly List<int> RandomNeighbourIndexList = new();
    private static readonly List<int> RandomNearbyCellIndexList = new();
    private static int2[] PositionListWith30Rings;

    #region GenericGrid

    public static int GetIndex(GridManager gridManager, int x, int y)
    {
        return GetIndex(gridManager.Height, x, y);
    }

    public static int GetIndex(int gridHeight, int x, int y)
    {
        return x * gridHeight + y;
    }

    public static void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        // gridManager currently only supports default origin-position (Vector3.zero) and default cellSize (1f)
        x = Mathf.FloorToInt(worldPosition.x);
        y = Mathf.FloorToInt(worldPosition.y);
    }

    public static Vector3 GetWorldPosition(int x, int y)
    {
        // gridManager currently only supports default origin-position (Vector3.zero) and default cellSize (1f)
        return new Vector3(x, y, 0);
    }

    public static bool IsPositionInsideGrid(GridManager gridManager, int x, int y)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < gridManager.Width &&
            y < gridManager.Height;
    }

    #endregion

    #region GridSearch_Neighbours

    public static bool CellsAreTouching(Vector3 worldPosition, int2 targetCell)
    {
        GetXY(worldPosition, out var centerX, out var centerY);
        return CellsAreTouching(centerX, centerY, targetCell.x, targetCell.y);
    }

    public static bool CellsAreTouching(int2 centerCell, int2 targetCell)
    {
        return CellsAreTouching(centerCell.x, centerCell.y, targetCell.x, targetCell.y);
    }

    public static bool CellsAreTouching(int centerX, int centerY, int targetX, int targetY)
    {
        var xDistance = Mathf.Abs(centerX - targetX);
        var yDistance = Mathf.Abs(centerY - targetY);
        var cellsAreSameOrNeighbours = xDistance is > -1 and < 2 && yDistance is > -1 and < 2;
        return cellsAreSameOrNeighbours;
    }

    public static void GetNeighbourCell(int index, int x, int y, out int neighbourX, out int neighbourY)
    {
        Assert.IsTrue(index >= 0 && index < 8, "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

        neighbourX = x + NeighbourDeltasX[index];
        neighbourY = y + NeighbourDeltasY[index];
    }

    private static bool TryGetValidNeighbourCell(GridManager gridManager, int x, int y, out int neighbourX, out int neighbourY)
    {
        InitializeRandomNeighbourIndexList(8);
        for (var j = 0; j < 8; j++)
        {
            var randomIndex = GetRandomNeighbourIndex();
            GetNeighbourCell(randomIndex, x, y, out neighbourX, out neighbourY);

            if (IsPositionInsideGrid(gridManager, neighbourX, neighbourY) &&
                gridManager.IsWalkable(neighbourX, neighbourY) &&
                !gridManager.IsOccupied(neighbourX, neighbourY))
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

    #endregion

    #region GridSearch_Other

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

    public static bool TryGetNearbyChoppingCell(GridManager gridManager, int2 currentTarget, out int2 newTarget, out int2 newPathTarget)
    {
        var nearbyCells = GetCellListAroundTargetCell30Rings(currentTarget.x, currentTarget.y);

        if (gridManager.IsDamageable(currentTarget.x, currentTarget.y) &&
            TryGetValidNeighbourCell(gridManager, currentTarget.x, currentTarget.y, out var newPathTargetX, out var newPathTargetY))
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

            if (!IsPositionInsideGrid(gridManager, x, y) ||
                !gridManager.IsDamageable(x, y))
            {
                continue;
            }

            if (TryGetValidNeighbourCell(gridManager, x, y, out newPathTargetX, out newPathTargetY))
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

    private static void InitializeRandomNearbyCellIndexList(int min, int max)
    {
        RandomNearbyCellIndexList.Clear();
        for (var i = min; i < max; i++)
        {
            RandomNearbyCellIndexList.Add(i);
        }
    }

    private static bool RandomNearbyCellIndexListIsEmpty()
    {
        return RandomNearbyCellIndexList.Count <= 0;
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

    #endregion
}