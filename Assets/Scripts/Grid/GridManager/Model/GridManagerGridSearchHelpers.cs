using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Grid
{
    public partial struct GridManager
    {
        #region GridSearchHelpers

        public bool TryGetVacantHorizontalNeighbour(int2 cell, out int2 neighbour)
        {
            var leftNeighbour = new int2(cell.x - 1, cell.y);
            var rightNeighbour = new int2(cell.x + 1, cell.y);

            var checkLeftFirst = Random.NextBool();
            var firstNeighbour = checkLeftFirst ? leftNeighbour : rightNeighbour;
            if (IsVacantCell(firstNeighbour))
            {
                neighbour = firstNeighbour;
                return true;
            }

            var secondNeighbour = checkLeftFirst ? rightNeighbour : leftNeighbour;
            if (IsVacantCell(secondNeighbour))
            {
                neighbour = secondNeighbour;
                return true;
            }

            neighbour = -1;
            return false;
        }

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

        public bool TryGetClosestWalkableNeighbourOfTarget(int2 selfCell, int2 targetCell,
            out int2 closestNeighbourCell)
        {
            var shortestDistance = float.MaxValue;
            closestNeighbourCell = -1;
            for (var j = 0; j < 8; j++)
            {
                var neighbourCell = GetNeighbourCell(j, targetCell);
                if (!IsPositionInsideGrid(neighbourCell) || !IsWalkable(neighbourCell) || !IsMatchingSection(selfCell, neighbourCell))
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

        public bool TryGetClosestChoppingCellSemiRandom(int2 selfCell, Entity selfEntity, out int2 availableChoppingCell,
            bool isDebugging)
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

            if (isDebugging)
            {
                Debug.LogWarning("No available chopping cell was found within the search-range");
            }

            availableChoppingCell = -1;
            return false;
        }

        public bool TryGetClosestBedSemiRandom(int2 center, out int2 availableBed, bool isDebugging)
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

            if (isDebugging)
            {
                Debug.LogWarning("No available bed was found within the search-range");
            }

            availableBed = new int2(-1, -1);
            return false;
        }

        public bool TryGetClosestBonfireSeatSemiRandom(int2 selfCell, Entity selfEntity, out int2 availableBonfireSeat,
            bool isDebugging)
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

                    var bonfireCell = selfCell + RelativePositionList[currentIndex];
                    if (IsBonfire(bonfireCell) &&
                        TryGetClosestValidNeighbourOfTarget(selfCell, selfEntity, bonfireCell, out var bonfireNeighbour))
                    {
                        availableBonfireSeat = bonfireNeighbour;
                        return true;
                    }
                }
            }

            if (isDebugging)
            {
                Debug.LogWarning("No available bonfire seat was found within the search-range");
            }

            availableBonfireSeat = -1;
            return false;
        }

        public bool TryGetEmptyCellNearCurrentTarget(int2 self, int2 oldTarget, out int2 newTarget, bool isDebugging = false, int radius = -1)
        {
            Assert.IsTrue(radius < RelativePositionRingInfoList.Length);
            if (radius < 1)
            {
                radius = RelativePositionRingInfoList.Length;
            }

            for (var ring = 1; ring < radius; ring++)
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

                    var relativePosition = oldTarget + RelativePositionList[currentIndex];
                    if (IsPositionInsideGrid(relativePosition) &&
                        IsEmptyCell(relativePosition) &&
                        IsMatchingSection(self, relativePosition))
                    {
                        newTarget = relativePosition;
                        return true;
                    }
                }
            }

            if (isDebugging)
            {
                Debug.LogError("No empty cell was found near current target");
            }

            newTarget = new int2(-1, -1);
            return false;
        }

        public bool TryGetNearbyEmptyCellSemiRandom(int2 center,
            out int2 nearbyCell,
            bool isDebugging = false,
            bool ignoreSection = false,
            int radius = -1)
        {
            Assert.IsTrue(radius < RelativePositionRingInfoList.Length);
            if (radius < 1)
            {
                radius = RelativePositionRingInfoList.Length;
            }

            for (var ring = 1; ring < radius; ring++)
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
                    if (IsPositionInsideGrid(relativePosition) &&
                        IsEmptyCell(relativePosition) &&
                        (ignoreSection || IsMatchingSection(center, relativePosition)))
                    {
                        nearbyCell = relativePosition;
                        return true;
                    }
                }
            }

            if (isDebugging)
            {
                Debug.LogError("No nearby empty cell was found");
            }

            nearbyCell = new int2(-1, -1);
            return false;
        }

        public bool TryGetClosestVacantCell(int2 ownCell, int2 targetCell, out int2 nearbyCell, bool includeTarget = true)
        {
            if (includeTarget && IsPositionInsideGrid(targetCell) && IsWalkable(targetCell) && !IsOccupied(targetCell))
            {
                nearbyCell = targetCell;
                return true;
            }

            // TODO: De-prioritize corners, since they have a higher travel-cost
            for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
            {
                var ringStart = RelativePositionRingInfoList[ring].x;
                var ringEnd = RelativePositionRingInfoList[ring].y;

                for (var i = ringStart; i < ringEnd; i++)
                {
                    var relativePosition = targetCell + RelativePositionList[i];
                    if (IsPositionInsideGrid(relativePosition) &&
                        IsWalkable(relativePosition) &&
                        !IsOccupied(relativePosition) &&
                        IsMatchingSection(relativePosition, ownCell))
                    {
                        nearbyCell = relativePosition;
                        return true;
                    }
                }
            }

            Debug.LogError("No nearby vacant cell was found");

            nearbyCell = new int2(-1, -1);
            return false;
        }

        public bool TryGetClosestWalkableCell(int2 center, out int2 nearbyCell, bool includeSelf = true, bool ignoreSection = true)
        {
            if (includeSelf && IsPositionInsideGrid(center) && IsWalkable(center))
            {
                nearbyCell = center;
                return true;
            }

            // TODO: De-prioritize corners, since they have a higher travel-cost
            for (var ring = 1; ring < RelativePositionRingInfoList.Length; ring++)
            {
                var ringStart = RelativePositionRingInfoList[ring].x;
                var ringEnd = RelativePositionRingInfoList[ring].y;

                for (var i = ringStart; i < ringEnd; i++)
                {
                    var relativePosition = center + RelativePositionList[i];
                    if (IsPositionInsideGrid(relativePosition) && IsWalkable(relativePosition) &&
                        (ignoreSection || IsMatchingSection(center, relativePosition)))
                    {
                        nearbyCell = relativePosition;
                        return true;
                    }
                }
            }

            Debug.LogError("No nearby walkable cell was found");

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
            Unity.Assertions.Assert.IsTrue(index >= 0 && index < 8,
                "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

            neighbourX = x + NeighbourDeltas[index].x;
            neighbourY = y + NeighbourDeltas[index].y;
        }

        private int GetSemiRandomIndex(int minInclusive, int maxExclusive)
        {
            return Random.NextInt(minInclusive, maxExclusive);
        }

        #endregion

        public int GetSectionOfNeighbour(int2 center)
        {
            var neighbourCell = GetAnyWalkableNeighbourCell(center);
            return neighbourCell.x > -1 ? GetSection(neighbourCell) : -1;
        }

        public int2 GetAnyWalkableNeighbourCell(int2 cell)
        {
            for (var i = 0; i < 8; i++)
            {
                var neighbourCell = GetNeighbourCell(i, cell);
                if (IsPositionInsideGrid(neighbourCell) && IsWalkable(neighbourCell))
                {
                    return neighbourCell;
                }
            }

            return -1;
        }
    }
}