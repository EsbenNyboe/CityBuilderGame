using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial struct GridManager
{
    #region GridSearchHelpers

    public bool TryGetNeighbouringTreeCell(int2 center, out int2 neighbouringTreeCell)
    {
        var randomStartIndex = GetSemiRandomIndex(0, 8);
        var currentIndex = randomStartIndex + 1;

        while (currentIndex != randomStartIndex)
        {
            currentIndex++;
            if (currentIndex >= 8)
            {
                currentIndex = 0;
            }

            var neighbourCell = GetNeighbourCell(currentIndex, center);

            if (IsPositionInsideGrid(neighbourCell) &&
                IsDamageable(neighbourCell))
            {
                neighbouringTreeCell = neighbourCell;
                return true;
            }
        }

        neighbouringTreeCell = -1;
        return false;
    }

    public bool TryGetClosestValidNeighbourOfTarget(int2 selfCell, Entity selfEntity, int2 targetCell,
        out int2 closestNeighbourCell)
    {
        var shortestDistance = float.MaxValue;
        closestNeighbourCell = -1;
        for (var j = 0; j < 8; j++)
        {
            var neighbourCell = GetNeighbourCell(j, targetCell);
            if (!IsVacantCell(neighbourCell, selfEntity) || !IsMatchingSection(selfCell, neighbourCell))
            {
                continue;
            }

            var distance = math.distance(selfCell, neighbourCell);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestNeighbourCell = neighbourCell;
            }
        }

        return closestNeighbourCell.x > -1;
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

    public bool TryGetClosestChoppingCellSemiRandom(int2 selfCell, Entity selfEntity, out int2 availableChoppingCell)
    {
        for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
        {
            var ringStart = RelativePositionRingInfoList[ring].x;
            var ringEnd = RelativePositionRingInfoList[ring].y;
            var randomStartIndex = GetSemiRandomIndex(ringStart, ringEnd);
            var currentIndex = randomStartIndex + 1;

            while (currentIndex != randomStartIndex)
            {
                currentIndex++;
                if (currentIndex >= ringEnd)
                {
                    currentIndex = ringStart;
                }

                var treeCell = selfCell + RelativePositionList[currentIndex];
                if (IsTree(treeCell) &&
                    TryGetClosestValidNeighbourOfTarget(selfCell, selfEntity, treeCell, out var treeNeighbour))
                {
                    availableChoppingCell = treeNeighbour;
                    return true;
                }
            }
        }

        DebugHelper.LogWarning("No available chopping cell was found within the search-range");

        availableChoppingCell = -1;
        return false;
    }

    public bool TryGetClosestBedSemiRandom(int2 center, out int2 availableBed)
    {
        for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
        {
            var ringStart = RelativePositionRingInfoList[ring].x;
            var ringEnd = RelativePositionRingInfoList[ring].y;
            var randomStartIndex = GetSemiRandomIndex(ringStart, ringEnd);
            var currentIndex = randomStartIndex + 1;

            while (currentIndex != randomStartIndex)
            {
                currentIndex++;
                if (currentIndex >= ringEnd)
                {
                    currentIndex = ringStart;
                }

                var targetCell = center + RelativePositionList[currentIndex];
                if (IsAvailableBed(targetCell) && IsMatchingSection(center, targetCell))
                {
                    availableBed = targetCell;
                    return true;
                }
            }
        }

        DebugHelper.LogWarning("No available bed was found within the search-range");

        availableBed = new int2(-1, -1);
        return false;
    }

    public bool TryGetNearbyEmptyCellSemiRandom(int2 center, out int2 nearbyCell)
    {
        for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
        {
            var ringStart = RelativePositionRingInfoList[ring].x;
            var ringEnd = RelativePositionRingInfoList[ring].y;
            var randomStartIndex = GetSemiRandomIndex(ringStart, ringEnd);
            var currentIndex = randomStartIndex + 1;

            while (currentIndex != randomStartIndex)
            {
                currentIndex++;
                if (currentIndex >= ringEnd)
                {
                    currentIndex = ringStart;
                }

                var relativePosition = center + RelativePositionList[currentIndex];
                if (IsEmptyCell(relativePosition) && IsMatchingSection(center, relativePosition))
                {
                    nearbyCell = relativePosition;
                    return true;
                }
            }
        }

        DebugHelper.LogError("No nearby empty cell was found");

        nearbyCell = new int2(-1, -1);
        return false;
    }


    public void PopulateRelativePositionList(int relativePositionListRadius)
    {
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

    public int2 GetNeighbourCell(int index, int2 cell)
    {
        GetNeighbourCell(index, cell.x, cell.y, out var neighbourX, out var neighbourY);
        return new int2(neighbourX, neighbourY);
    }

    public void GetNeighbourCell(int index, int x, int y, out int neighbourX, out int neighbourY)
    {
        Assert.IsTrue(index >= 0 && index < 8,
            "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

        neighbourX = x + NeighbourDeltas[index].x;
        neighbourY = y + NeighbourDeltas[index].y;
    }

    private int GetSemiRandomIndex(int minInclusive, int maxExclusive)
    {
        return Random.NextInt(minInclusive, maxExclusive);
    }

    #endregion
}