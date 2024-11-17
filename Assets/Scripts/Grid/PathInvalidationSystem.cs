using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Grid
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(GridManagerHighLevelSortingSystem))]
    public partial struct PathInvalidationSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            if (!gridManager.WalkableGridIsDirty)
            {
                return;
            }

            using var invalidatedCellsQueue = new NativeQueue<int2>(Allocator.Temp);
            var length = gridManager.WalkableGrid.Length;
            for (var i = 0; i < length; i++)
            {
                if (gridManager.WalkableGrid[i].IsDirty && !gridManager.IsWalkable(i))
                {
                    invalidatedCellsQueue.Enqueue(gridManager.GetXY(i));
                }
            }

            var numberOfInvalidatedCells = invalidatedCellsQueue.Count;
            using var invalidatedCells = new NativeHashSet<int2>(numberOfInvalidatedCells, Allocator.Temp);
            for (var i = 0; i < numberOfInvalidatedCells; i++)
            {
                invalidatedCells.Add(invalidatedCellsQueue.Dequeue());
            }

            foreach (var (pathFollow, pathPositions, entity) in SystemAPI.Query<RefRW<PathFollow>, DynamicBuffer<PathPosition>>().WithEntityAccess())
            {
                var pathIndex = pathFollow.ValueRO.PathIndex;
                if (CurrentPathIsInvalidated(pathPositions, invalidatedCells, pathIndex))
                {
                    var currentPathPosition = pathPositions[pathIndex].Position;
                    var targetPathPosition = pathPositions[0].Position;
                    if (gridManager.IsMatchingSection(currentPathPosition, targetPathPosition))
                    {
                        PathHelpers.TrySetPath(ecb, entity, currentPathPosition, targetPathPosition);
                    }
                    else
                    {
                        // TODO: Insert logic for finding random nearby path-target
                        DebugHelper.Log("Current path is no longer possible. TODO: Insert logic for finding a random nearby path-target.");
                    }
                }
            }
        }

        private static bool CurrentPathIsInvalidated(DynamicBuffer<PathPosition> pathPositions, NativeHashSet<int2> invalidatedCells, int pathIndex)
        {
            for (var i = pathIndex; i > -1; i--)
            {
                if (invalidatedCells.Contains(pathPositions[i].Position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}