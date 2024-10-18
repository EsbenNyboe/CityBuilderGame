using System.Collections.Generic;
using Unity.Entities;
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

    public static int GetIndex(GridManager gridManager, Vector3 worldPosition)
    {
        GetXY(worldPosition, out var x, out var y);
        return GetIndex(gridManager.Height, x, y);
    }

    public static int GetIndex(GridManager gridManager, int x, int y)
    {
        return GetIndex(gridManager.Height, x, y);
    }

    public static int GetIndex(int gridHeight, int x, int y)
    {
        return x * gridHeight + y;
    }

    public static void GetXY(GridManager gridManager, int gridIndex, out int x, out int y)
    {
        var height = gridManager.Height;

        x = gridIndex / height;
        y = gridIndex % height;
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

    public static void ValidateGridPosition(GridManager gridManager, ref int x, ref int y)
    {
        x = math.clamp(x, 0, gridManager.Width - 1);
        y = math.clamp(y, 0, gridManager.Height - 1);
    }

    public static bool IsPositionInsideGrid(GridManager gridManager, int2 cell)
    {
        return IsPositionInsideGrid(gridManager, cell.x, cell.y);
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
                IsWalkable(gridManager, neighbourX, neighbourY) &&
                !IsOccupied(gridManager, neighbourX, neighbourY))
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

        if (IsDamageable(gridManager, currentTarget.x, currentTarget.y) &&
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
                !IsDamageable(gridManager, x, y))
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

    #region WalkableGrid

    public static bool IsWalkable(GridManager gridManager, Vector3 position)
    {
        GetXY(position, out var x, out var y);
        return IsWalkable(gridManager, x, y);
    }

    public static bool IsWalkable(GridManager gridManager, int2 cell)
    {
        return IsWalkable(gridManager, cell.x, cell.y);
    }

    public static bool IsWalkable(GridManager gridManager, int x, int y)
    {
        return IsWalkable(gridManager, GetIndex(gridManager.Height, x, y));
    }

    public static bool IsWalkable(GridManager gridManager, int i)
    {
        return gridManager.WalkableGrid[i].IsWalkable;
    }

    public static void SetIsWalkable(ref GridManager gridManager, Vector3 position, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        GetXY(position, out var x, out var y);
        SetIsWalkable(ref gridManager, x, y, isWalkable);
    }

    public static void SetIsWalkable(ref GridManager gridManager, int2 cell, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        SetIsWalkable(ref gridManager, cell.x, cell.y, isWalkable);
    }

    public static void SetIsWalkable(ref GridManager gridManager, int x, int y, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        var gridIndex = GetIndex(gridManager, x, y);
        SetIsWalkable(ref gridManager, gridIndex, isWalkable);
    }

    public static void SetIsWalkable(ref GridManager gridManager, int i, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        var walkableCell = gridManager.WalkableGrid[i];
        walkableCell.IsWalkable = isWalkable;
        walkableCell.IsDirty = true;
        gridManager.WalkableGrid[i] = walkableCell;
        gridManager.WalkableGridIsDirty = true;
    }

    #endregion

    #region OccupiableGrid

    public static bool IsOccupied(GridManager gridManager, Vector3 position)
    {
        GetXY(position, out var x, out var y);
        return IsOccupied(gridManager, x, y);
    }

    public static bool IsOccupied(GridManager gridManager, int2 cell)
    {
        return IsOccupied(gridManager, cell.x, cell.y);
    }

    public static bool IsOccupied(GridManager gridManager, int x, int y)
    {
        var gridIndex = GetIndex(gridManager, x, y);
        return IsOccupied(gridManager, gridIndex);
    }

    public static bool IsOccupied(GridManager gridManager, int gridIndex)
    {
        var occupant = gridManager.OccupiableGrid[gridIndex].Occupant;
        return occupant != Entity.Null && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(occupant);
    }

    public static bool EntityIsOccupant(GridManager gridManager, int i, Entity entity)
    {
        return gridManager.OccupiableGrid[i].Occupant == entity;
    }

    public static bool TryClearOccupant(ref GridManager gridManager, Vector3 position, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        GetXY(position, out var x, out var y);
        return TryClearOccupant(ref gridManager, x, y, entity);
    }

    public static bool TryClearOccupant(ref GridManager gridManager, int2 cell, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        return TryClearOccupant(ref gridManager, cell.x, cell.y, entity);
    }

    public static bool TryClearOccupant(ref GridManager gridManager, int x, int y, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var i = GetIndex(gridManager, x, y);
        return TryClearOccupant(ref gridManager, i, entity);
    }

    public static bool TryClearOccupant(ref GridManager gridManager, int i, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        if (EntityIsOccupant(gridManager, i, entity))
        {
            SetOccupant(ref gridManager, i, Entity.Null);
            return true;
        }

        return false;
    }

    public static void SetOccupant(ref GridManager gridManager, int x, int y, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var gridIndex = GetIndex(gridManager, x, y);
        SetOccupant(ref gridManager, gridIndex, entity);
    }

    public static void SetOccupant(ref GridManager gridManager, int i, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var occupiableCell = gridManager.OccupiableGrid[i];
        occupiableCell.Occupant = entity;
        occupiableCell.IsDirty = true;
        gridManager.OccupiableGrid[i] = occupiableCell;
        gridManager.OccupiableGridIsDirty = true;
    }

    #endregion

    #region DamageableGrid

    public static bool IsDamageable(GridManager gridManager, Vector3 position)
    {
        GetXY(position, out var x, out var y);
        return IsDamageable(gridManager, x, y);
    }

    public static bool IsDamageable(GridManager gridManager, int2 cell)
    {
        return IsDamageable(gridManager, cell.x, cell.y);
    }

    public static bool IsDamageable(GridManager gridManager, int x, int y)
    {
        var i = GetIndex(gridManager, x, y);
        return IsDamageable(gridManager, i);
    }

    public static bool IsDamageable(GridManager gridManager, int i)
    {
        return gridManager.DamageableGrid[i].Health > 0;
    }

    public static void AddDamage(ref GridManager gridManager, int i, float damage)
    {
        // Note: Remember to call SetComponent after this method
        SetHealth(ref gridManager, i, gridManager.DamageableGrid[i].Health - damage);
    }

    public static void SetHealthToMax(ref GridManager gridManager, int i)
    {
        // Note: Remember to call SetComponent after this method
        SetHealth(ref gridManager, i, gridManager.DamageableGrid[i].MaxHealth);
    }

    public static void SetHealth(ref GridManager gridManager, int i, float health)
    {
        // Note: Remember to call SetComponent after this method
        var damageableCell = gridManager.DamageableGrid[i];
        damageableCell.Health = health;
        damageableCell.IsDirty = true;
        gridManager.DamageableGrid[i] = damageableCell;
        gridManager.DamageableGridIsDirty = true;
    }

    #endregion
}