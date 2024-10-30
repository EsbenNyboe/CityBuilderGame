using Unity.Assertions;
using Unity.Collections;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public partial struct GridManager
{
    #region GridSearchHelpers

    public bool TryGetNearbyChoppingCell(int2 currentTarget, out int2 newTarget, out int2 newPathTarget)
    {
        var nearbyCells = GetCachedCellListAroundTargetCell(currentTarget.x, currentTarget.y);

        if (IsDamageable(currentTarget.x, currentTarget.y) &&
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
                !IsDamageable(x, y))
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

    private bool TryGetValidNeighbourCell(int x, int y, out int neighbourX, out int neighbourY)
    {
        RandomizeNeighbourSequenceIndex();
        for (var j = 0; j < 8; j++)
        {
            GetNeighbourCell(GetNextNeighbourSequenceIndex(), x, y, out neighbourX, out neighbourY);

            if (IsPositionInsideGrid(neighbourX, neighbourY) &&
                IsWalkable(neighbourX, neighbourY) &&
                !IsOccupied(neighbourX, neighbourY))
            {
                return true;
            }
        }

        neighbourX = -1;
        neighbourY = -1;
        return false;
    }

    public bool TryGetNearbyVacantCell(int x, int y, out int2 nearbyCell)
    {
        var movePositionList = GetCachedCellListAroundTargetCell(x, y);
        for (var i = 1; i < movePositionList.Length; i++)
        {
            nearbyCell = movePositionList[i];
            if (IsPositionInsideGrid(nearbyCell) && IsWalkable(nearbyCell) &&
                !IsOccupied(nearbyCell))
            {
                return true;
            }
        }

        nearbyCell = default;
        return false;
    }

    public NativeArray<int2> GetCachedCellListAroundTargetCell(int targetX, int targetY)
    {
        var ringCount = PositionListLength;
        var index = 0;
        // include the target-cell
        var cell = PositionList[index];
        cell.x = targetX;
        cell.y = targetY;
        PositionList[index] = cell;

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
                cell.x = x;
                cell.y = y;
                PositionList[index] = cell;
            }

            // go to min Y
            while (y > targetY - ringSize)
            {
                index++;
                y--;
                cell.x = x;
                cell.y = y;
                PositionList[index] = cell;
            }

            // go to max X
            while (x < targetX + ringSize)
            {
                index++;
                x++;
                cell.x = x;
                cell.y = y;
                PositionList[index] = cell;
            }

            // go to max Y
            while (y < targetY + ringSize)
            {
                index++;
                y++;
                cell.x = x;
                cell.y = y;
                PositionList[index] = cell;
            }
        }

        return PositionList;
    }

    public void RandomizeNeighbourSequenceIndex()
    {
        NeighbourSequenceIndex = Random.Range(0, 8);
    }

    private int GetNextNeighbourSequenceIndex()
    {
        NeighbourSequenceIndex++;
        if (NeighbourSequenceIndex >= 8)
        {
            NeighbourSequenceIndex = 0;
        }

        return NeighbourSequenceIndex;
    }

    public void GetSequencedNeighbourCell(int x, int y, out int neighbourX, out int neighbourY)
    {
        GetNeighbourCell(GetNextNeighbourSequenceIndex(), x, y, out neighbourX, out neighbourY);
    }

    public void GetNeighbourCell(int index, int x, int y, out int neighbourX, out int neighbourY)
    {
        Assert.IsTrue(index >= 0 && index < 8, "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

        neighbourX = x + NeighbourDeltas[index].x;
        neighbourY = y + NeighbourDeltas[index].y;
    }

    private void InitializeRandomNearbyCellIndexList(int min, int max)
    {
        RandomNearbyCellIndexList.Clear();
        for (var i = min; i < max; i++)
        {
            RandomNearbyCellIndexList.Add(i);
        }
    }

    private bool RandomNearbyCellIndexListIsEmpty()
    {
        return RandomNearbyCellIndexList.Length <= 0;
    }

    private int GetRandomNearbyCellIndex()
    {
        var indexListIndex = Random.Range(0, RandomNearbyCellIndexList.Length);
        var cellListIndex = RandomNearbyCellIndexList[indexListIndex];
        RandomNearbyCellIndexList.RemoveAt(indexListIndex);
        return cellListIndex;
    }

    #endregion
}