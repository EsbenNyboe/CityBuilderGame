using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.Talking
{
    public static class TalkingHelpers
    {
        public static bool TryGetNeighbourWithComponent<T>(GridManager gridManager, int2 cell,
            ComponentLookup<T> lookup, out int2 neighbour) where T : unmanaged, IComponentData
        {
            var leftNeighbour = new int2(cell.x - 1, cell.y);
            var rightNeighbour = new int2(cell.x + 1, cell.y);

            var checkLeftFirst = gridManager.Random.NextBool();
            var firstNeighbourToCheck = checkLeftFirst ? leftNeighbour : rightNeighbour;
            var secondNeighbourToCheck = checkLeftFirst ? rightNeighbour : leftNeighbour;

            if (gridManager.IsPositionInsideGrid(firstNeighbourToCheck) &&
                gridManager.TryGetOccupant(firstNeighbourToCheck, out var neighbourEntity) &&
                lookup.HasComponent(neighbourEntity))
            {
                neighbour = firstNeighbourToCheck;
                return true;
            }

            if (gridManager.IsPositionInsideGrid(secondNeighbourToCheck) &&
                gridManager.TryGetOccupant(secondNeighbourToCheck, out neighbourEntity) &&
                lookup.HasComponent(neighbourEntity))
            {
                neighbour = secondNeighbourToCheck;
                return true;
            }

            neighbour = -1;
            return false;
        }
    }
}