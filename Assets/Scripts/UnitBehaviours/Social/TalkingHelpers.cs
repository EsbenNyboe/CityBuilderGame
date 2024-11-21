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
            if (gridManager.TryGetOccupant(leftNeighbour, out var neighbourEntity) &&
                lookup.HasComponent(neighbourEntity))
            {
                neighbour = leftNeighbour;
                return true;
            }

            if (gridManager.TryGetOccupant(rightNeighbour, out neighbourEntity) &&
                lookup.HasComponent(neighbourEntity))
            {
                neighbour = rightNeighbour;
                return true;
            }

            neighbour = -1;
            return false;
        }
    }
}