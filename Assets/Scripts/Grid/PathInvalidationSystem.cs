using Debugging;
using UnitBehaviours;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Grid
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(GridManagerSectionSortingSystem))]
    public partial struct PathInvalidationSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var isDebuggingPathInvalidation = debugToggleManager.DebugPathInvalidation;
            var isDebuggingPath = debugToggleManager.DebugPathfinding;
            var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;

            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();
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

            foreach (var (localTransform, pathFollow, pathPositions, socialRelationships, moodInitiative, entity) in
                     SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<PathFollow>, DynamicBuffer<PathPosition>,
                             RefRW<SocialRelationships>,
                             RefRW<MoodInitiative>>()
                         .WithEntityAccess())
            {
                if (!CurrentPathIsInvalidated(pathPositions, invalidatedCells, pathFollow.ValueRO.PathIndex))
                {
                    continue;
                }

                InvalidatePath(ecb, gridManager, entity, localTransform, pathFollow, pathPositions,
                    socialDynamicsManager, socialRelationships, moodInitiative, isDebuggingPathInvalidation,
                    isDebuggingPath, isDebuggingSearch);
            }
        }

        private static bool CurrentPathIsInvalidated(DynamicBuffer<PathPosition> pathPositions,
            NativeHashSet<int2> invalidatedCells, int pathIndex)
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

        private static void InvalidatePath(  EntityCommandBuffer ecb, GridManager gridManager, Entity entity,
            RefRW<LocalTransform> localTransform, RefRW<PathFollow> pathFollow,
            DynamicBuffer<PathPosition> pathPositions, SocialDynamicsManager socialDynamicsManager,
            RefRW<SocialRelationships> socialRelationships,
            RefRW<MoodInitiative> moodInitiative, bool isDebuggingPathInvalidation, bool isDebuggingPath,
            bool isDebuggingSearch)
        {
            var pathIndex = pathFollow.ValueRO.PathIndex;
            var nextPathNode = pathPositions[pathIndex].Position;
            var nextPathNodeIsWalkable = gridManager.IsWalkable(nextPathNode);
            var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);
            var currentCellIsWalkable = gridManager.IsWalkable(currentCell);

            var targetCell = pathPositions[0].Position;
            var targetCellIsWalkable = gridManager.IsWalkable(targetCell);

            var pathIsPossible = gridManager.IsMatchingSection(currentCell, targetCell);

            if (currentCellIsWalkable && targetCellIsWalkable && pathIsPossible)
            {
                // My target is still reachable. I'll find a better path.
                if (nextPathNodeIsWalkable)
                {
                    // I can keep my momentum, and adjust my path, after the next step!
                    PathHelpers.TrySetPath(ecb, entity, nextPathNode, targetCell, isDebuggingPath);
                }
                else
                {
                    // I'll need to do a hard stop, and pivot.
                    PathHelpers.TrySetPath(ecb, entity, currentCell, targetCell, isDebuggingPath);
                }
            }
            else
            {
                // My target is no longer reachable. I'll try and find a place close to my target...

                if (InvalidationCausedUnitToLoseAccessToBeds(gridManager, entity, targetCell))
                {
                    GetAngryAtOccupant(gridManager, socialDynamicsManager, socialRelationships, targetCell);
                }

                if (currentCellIsWalkable &&
                    gridManager.TryGetNearbyEmptyCellSemiRandom(targetCell, out targetCell, isDebuggingSearch))
                {
                    if (isDebuggingPathInvalidation)
                    {
                        Debug.Log("Invalidation: I'll walk to a nearby spot");
                    }

                    // I'll walk to a nearby spot...
                    if (nextPathNodeIsWalkable)
                    {
                        PathHelpers.TrySetPath(ecb, entity, nextPathNode, targetCell, isDebuggingPath);
                    }
                    else
                    {
                        PathHelpers.TrySetPath(ecb, entity, currentCell, targetCell, isDebuggingPath);
                    }

                    // I should do a new search ASAP.
                    moodInitiative.ValueRW.Initiative += 1f;
                }
                else if (!currentCellIsWalkable &&
                         (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out targetCell, isDebuggingSearch,
                              true,
                              10) ||
                          gridManager.TryGetClosestWalkableCell(currentCell, out targetCell)))
                {
                    if (isDebuggingPathInvalidation)
                    {
                        Debug.Log("Invalidation: I'll DEFY PHYSICS");
                    }

                    // I'll defy physics, and move to a nearby spot.
                    pathPositions.Clear();
                    pathPositions.Add(new PathPosition { Position = targetCell });
                    pathFollow.ValueRW.PathIndex = 0;
                }
                else
                {
                    if (isDebuggingPathInvalidation)
                    {
                        Debug.Log("Invalidation: I'm stuck here...");
                    }

                    // I'm stuck... I guess, I'll just stand here, then...
                    var newTarget = pathPositions[0].Position;
                    pathPositions.Clear();
                    pathPositions.Add(new PathPosition { Position = newTarget });
                    pathFollow.ValueRW.PathIndex = 0;
                }
            }
        }

        private static void GetAngryAtOccupant(GridManager gridManager, SocialDynamicsManager socialDynamicsManager,
            RefRW<SocialRelationships> socialRelationships, int2 targetCell)
        {
            var occupant = gridManager.GetOccupant(targetCell);
            var fondnessOfBedOccupant = socialRelationships.ValueRO.Relationships[occupant];
            fondnessOfBedOccupant += socialDynamicsManager.ImpactOnBedBeingOccupied;
            socialRelationships.ValueRW.Relationships[occupant] = fondnessOfBedOccupant;
        }

        private static bool InvalidationCausedUnitToLoseAccessToBeds(GridManager gridManager, Entity entity,
            int2 targetCell)
        {
            return gridManager.IsBed(targetCell) && gridManager.IsOccupied(targetCell, entity) &&
                   !gridManager.TryGetClosestBedSemiRandom(targetCell, out _, true);
        }
    }
}