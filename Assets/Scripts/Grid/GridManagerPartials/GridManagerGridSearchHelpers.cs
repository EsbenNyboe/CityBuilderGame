using Unity.Assertions;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
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

    public bool TryGetNeighbouringTreeCell(int x, int y, out int treeX, out int treeY)
    {
        RandomizeNeighbourSequenceIndex();
        for (var j = 0; j < 8; j++)
        {
            GetNeighbourCell(GetNextNeighbourSequenceIndex(), x, y, out treeX, out treeY);

            if (IsPositionInsideGrid(treeX, treeY) &&
                IsDamageable(treeX, treeY))
            {
                return true;
            }
        }

        treeX = -1;
        treeY = -1;
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
        var ringCount = PositionListRadius;
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

    public int2 GetNearbyEmptyCell(int2 center)
    {
        for (var i = 1; i < RelativePositionList.Length; i++)
        {
            if (IsEmptyCell(center + RelativePositionList[i]))
            {
                return center + RelativePositionList[i];
            }
        }

        Debug.LogError("No position found");
        return new int2(-1, -1);
    }

    public int2 GetNearbyVacantCell(int2 center)
    {
        for (var i = 1; i < RelativePositionList.Length; i++)
        {
            if (IsVacantCell(center + RelativePositionList[i]))
            {
                return center + RelativePositionList[i];
            }
        }

        Debug.LogError("No position found");
        return new int2(-1, -1);
    }

    public bool TryGetClosestBedSemiRandom(int2 center, out int2 availableBed)
    {
        // TODO: Can this be refactored, so it doesn't duplicate so much code for every variant of this search-pattern?
        for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
        {
            var ringStart = RelativePositionRingInfoList[ring].x;
            var ringEnd = RelativePositionRingInfoList[ring].y;
            var randomStartIndex = Random.Range(ringStart, ringEnd);
            var currentIndex = randomStartIndex + 1;

            while (currentIndex != randomStartIndex)
            {
                currentIndex++;
                if (currentIndex >= ringEnd)
                {
                    currentIndex = ringStart;
                }

                if (IsAvailableBed(center + RelativePositionList[currentIndex]))
                {
                    availableBed = center + RelativePositionList[currentIndex];
                    return true;
                }
            }
        }

        Debug.LogWarning("No available bed was found within the search-range");
        availableBed = new int2(-1, -1);
        return false;
    }

    public bool TryGetNearbyEmptyCellSemiRandom(int2 center, out int2 nearbyCell)
    {
        for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
        {
            var ringStart = RelativePositionRingInfoList[ring].x;
            var ringEnd = RelativePositionRingInfoList[ring].y;
            var randomStartIndex = Random.Range(ringStart, ringEnd);
            var currentIndex = randomStartIndex + 1;

            while (currentIndex != randomStartIndex)
            {
                currentIndex++;
                if (currentIndex >= ringEnd)
                {
                    currentIndex = ringStart;
                }

                if (IsEmptyCell(center + RelativePositionList[currentIndex]))
                {
                    nearbyCell = center + RelativePositionList[currentIndex];
                    return true;
                }
            }
        }

        Debug.LogError("No nearby empty cell was found");
        nearbyCell = new int2(-1, -1);
        return false;
    }

    public void PopulateRelativePositionList(int relativePositionListRadius)
    {
        // TODO: Refactor and combine with PositionList-logic (or select one or the other)

        var ringCount = relativePositionListRadius;
        var index = 0;
        // the first index is the center position
        var cell = RelativePositionList[index];
        cell.x = 0;
        cell.y = 0;
        RelativePositionList[index] = cell;

        for (var ringSize = 1; ringSize < ringCount; ringSize++)
        {
            // start at max X & max Y
            var x = ringSize;
            var y = ringSize;

            // go to min X
            while (x > -ringSize)
            {
                index++;
                x--;
                cell.x = x;
                cell.y = y;
                RelativePositionList[index] = cell;
            }

            // go to min Y
            while (y > -ringSize)
            {
                index++;
                y--;
                cell.x = x;
                cell.y = y;
                RelativePositionList[index] = cell;
            }

            // go to max X
            while (x < ringSize)
            {
                index++;
                x++;
                cell.x = x;
                cell.y = y;
                RelativePositionList[index] = cell;
            }

            // go to max Y
            while (y < ringSize)
            {
                index++;
                y++;
                cell.x = x;
                cell.y = y;
                RelativePositionList[index] = cell;
            }

            var ringEndExclusive = index + 1;
            var ringStartInclusive = ringEndExclusive - ringSize * 8;
            RelativePositionRingInfoList[ringSize] = new int2(ringStartInclusive, ringEndExclusive);
        }
    }

    // Note: Remember to call SetComponent after this method
    public void RandomizeNeighbourSequenceIndex()
    {
        NeighbourSequenceIndex = Random.Range(0, 8);
    }

    // Note: Remember to call SetComponent after this method
    private int GetNextNeighbourSequenceIndex()
    {
        NeighbourSequenceIndex++;
        if (NeighbourSequenceIndex >= 8)
        {
            NeighbourSequenceIndex = 0;
        }

        return NeighbourSequenceIndex;
    }

    // Note: Remember to call SetComponent after this method
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