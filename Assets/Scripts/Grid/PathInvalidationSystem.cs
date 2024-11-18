using Debugging;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Grid
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(GridManagerHighLevelSortingSystem))]
    public partial struct PathInvalidationSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugPathfinding;
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

            foreach (var (localTransform, pathFollow, pathPositions, entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<PathFollow>, DynamicBuffer<PathPosition>>().WithEntityAccess())
            {
                var pathIndex = pathFollow.ValueRO.PathIndex;
                if (CurrentPathIsInvalidated(pathPositions, invalidatedCells, pathIndex))
                {
                    var currentPathPosition = pathPositions[pathIndex].Position;
                    var targetPathPosition = pathPositions[0].Position;
                    var positionIsWalkable = gridManager.IsWalkable(currentPathPosition);
                    var targetIsWalkable = gridManager.IsWalkable(targetPathPosition);
                    var pathIsPossible = gridManager.IsMatchingSection(currentPathPosition, targetPathPosition);

                    if (positionIsWalkable && targetIsWalkable && pathIsPossible)
                    {
                        // My target is still reachable. I'll find a better path.
                        PathHelpers.TrySetPath(ecb, entity, currentPathPosition, targetPathPosition, isDebugging);
                    }
                    else
                    {
                        // My target is no longer reachable. I'll try and find a nearby place to stand...
                        if (positionIsWalkable && gridManager.TryGetNearbyEmptyCellSemiRandom(currentPathPosition, out targetPathPosition))
                        {
                            // I'll walk to a nearby spot...
                            PathHelpers.TrySetPath(ecb, entity, currentPathPosition, targetPathPosition, isDebugging);
                        }
                        else if (!positionIsWalkable && gridManager.TryGetNearbyWalkableCellSemiRandom(currentPathPosition, out targetPathPosition))
                        {
                            // I'll defy physics, and move to a nearby spot.
                            pathPositions.Clear();
                            pathPositions.Add(new PathPosition { Position = targetPathPosition });
                            pathFollow.ValueRW.PathIndex = 0;
                        }
                        else
                        {
                            // I'm stuck... I guess, I'll just stand here, then...
                            var newTarget = pathPositions[0].Position;
                            pathPositions.Clear();
                            pathPositions.Add(new PathPosition { Position = newTarget });
                            pathFollow.ValueRW.PathIndex = 0;
                        }
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