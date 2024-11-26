using Debugging;
using Effects.SocialEffectsRendering;
using UnitBehaviours;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
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
            state.RequireForUpdate<SocialDebugManager>();
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
            var socialDebugManager = SystemAPI.GetSingleton<SocialDebugManager>();
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

            var invalidatedPathfindingEntities = new NativeList<Entity>(Allocator.TempJob);

            foreach (var (pathFollow, pathPositions, entity) in SystemAPI.Query<RefRO<PathFollow>, DynamicBuffer<PathPosition>>().WithEntityAccess())
            {
                if (CurrentPathIsInvalidated(pathPositions, invalidatedCells, pathFollow.ValueRO.PathIndex))
                {
                    invalidatedPathfindingEntities.Add(entity);
                }
            }

            var validatePathJobHandle = new ValidatePathJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                GridManager = gridManager,
                SocialDynamicsManager = socialDynamicsManager,
                SocialDebugManager = socialDebugManager,
                IsDebuggingPathInvalidation = isDebuggingPathInvalidation,
                IsDebuggingPath = isDebuggingPath,
                IsDebuggingSearch = isDebuggingSearch,
                Entities = invalidatedPathfindingEntities.AsArray(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                PathFollowLookup = SystemAPI.GetComponentLookup<PathFollow>(),
                PathPositionLookup = SystemAPI.GetBufferLookup<PathPosition>(),
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>(),
                MoodInitiativeLookup = SystemAPI.GetComponentLookup<MoodInitiative>(),
                MoodSleepinessLookup = SystemAPI.GetComponentLookup<MoodSleepiness>()
            }.Schedule(invalidatedPathfindingEntities.Length, 1);
            validatePathJobHandle.Complete();

            invalidatedPathfindingEntities.Dispose();
        }

        [BurstCompile]
        private struct ValidatePathJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
            [ReadOnly] public GridManager GridManager;
            [ReadOnly] public SocialDynamicsManager SocialDynamicsManager;
            [ReadOnly] public SocialDebugManager SocialDebugManager;
            [ReadOnly] public bool IsDebuggingPathInvalidation;
            [ReadOnly] public bool IsDebuggingPath;
            [ReadOnly] public bool IsDebuggingSearch;
            [ReadOnly] public NativeArray<Entity> Entities;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<PathFollow> PathFollowLookup;

            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<PathPosition> PathPositionLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<MoodInitiative> MoodInitiativeLookup;

            [ReadOnly] public ComponentLookup<MoodSleepiness> MoodSleepinessLookup;

            public void Execute(int index)
            {
                var entity = Entities[index];
                var pathFollow = PathFollowLookup[entity];
                var pathPosition = PathPositionLookup[entity];
                var localTransform = LocalTransformLookup[entity];
                var socialRelationships = SocialRelationshipsLookup[entity];
                var moodInitiative = MoodInitiativeLookup[entity];
                var moodSleepiness = MoodSleepinessLookup[entity];

                var nextPathNode = pathPosition[pathFollow.PathIndex].Position;
                var nextPathNodeIsWalkable = GridManager.IsWalkable(nextPathNode);
                var currentCell = GridHelpers.GetXY(localTransform.Position);
                var currentCellIsWalkable = GridManager.IsWalkable(currentCell);

                var targetCell = pathPosition[0].Position;
                var targetCellIsWalkable = GridManager.IsWalkable(targetCell);

                var pathIsPossible = GridManager.IsMatchingSection(currentCell, targetCell);

                if (currentCellIsWalkable && targetCellIsWalkable && pathIsPossible)
                {
                    // My target is still reachable. I'll find a better path.
                    if (nextPathNodeIsWalkable)
                    {
                        // I can keep my momentum, and adjust my path, after the next step!
                        PathHelpers.TrySetPath(EcbParallelWriter, index, entity, nextPathNode, targetCell, IsDebuggingPath);
                    }
                    else
                    {
                        // I'll need to do a hard stop, and pivot.
                        PathHelpers.TrySetPath(EcbParallelWriter, index, entity, currentCell, targetCell, IsDebuggingPath);
                    }
                }
                else
                {
                    // My target is no longer reachable. I'll try and find a place close to my target...

                    if (InvalidationCausedUnitToLoseAccessToBeds(GridManager, entity, targetCell))
                    {
                        GetAngryAtOccupant(EcbParallelWriter, index, GridManager, SocialDynamicsManager, SocialDebugManager, ref socialRelationships,
                            moodSleepiness,
                            localTransform.Position, targetCell);
                        SocialRelationshipsLookup[entity] = socialRelationships;
                    }

                    if (currentCellIsWalkable &&
                        GridManager.TryGetNearbyEmptyCellSemiRandom(targetCell, out targetCell, IsDebuggingSearch))
                    {
                        if (IsDebuggingPathInvalidation)
                        {
                            Debug.Log("Invalidation: I'll walk to a nearby spot");
                        }

                        // I'll walk to a nearby spot...
                        if (nextPathNodeIsWalkable)
                        {
                            PathHelpers.TrySetPath(EcbParallelWriter, index, entity, nextPathNode, targetCell, IsDebuggingPath);
                        }
                        else
                        {
                            PathHelpers.TrySetPath(EcbParallelWriter, index, entity, currentCell, targetCell, IsDebuggingPath);
                        }

                        // I should do a new search ASAP.
                        moodInitiative.Initiative += 1f;
                        MoodInitiativeLookup[entity] = moodInitiative;
                    }
                    else if (!currentCellIsWalkable &&
                             (GridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out targetCell, IsDebuggingSearch,
                                  true,
                                  10) ||
                              GridManager.TryGetClosestWalkableCell(currentCell, out targetCell)))
                    {
                        if (IsDebuggingPathInvalidation)
                        {
                            Debug.Log("Invalidation: I'll DEFY PHYSICS");
                        }

                        // I'll defy physics, and move to a nearby spot.
                        pathPosition.Clear();
                        pathPosition.Add(new PathPosition { Position = targetCell }); // TODO: Necessary to set lookup?
                        pathFollow.PathIndex = 0;
                        PathFollowLookup[entity] = pathFollow;
                    }
                    else
                    {
                        if (IsDebuggingPathInvalidation)
                        {
                            Debug.Log("Invalidation: I'm stuck here...");
                        }

                        // I'm stuck... I guess, I'll just stand here, then...
                        var newTarget = pathPosition[0].Position;
                        pathPosition.Clear();
                        pathPosition.Add(new PathPosition { Position = newTarget });
                        pathFollow.PathIndex = 0;
                        PathFollowLookup[entity] = pathFollow;
                    }
                }
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

        private static void GetAngryAtOccupant(EntityCommandBuffer.ParallelWriter ecbParallelWriter, int index, GridManager gridManager,
            SocialDynamicsManager socialDynamicsManager,
            SocialDebugManager socialDebugManager,
            ref SocialRelationships socialRelationships, MoodSleepiness moodSleepiness, float3 position,
            int2 targetCell)
        {
            var occupant = gridManager.GetOccupant(targetCell);
            var fondnessOfBedOccupant = socialRelationships.Relationships[occupant];
            var influenceAmount = socialDynamicsManager.ImpactOnBedBeingOccupied * moodSleepiness.Sleepiness;
            fondnessOfBedOccupant += influenceAmount;
            socialRelationships.Relationships[occupant] = fondnessOfBedOccupant;

            if (socialDebugManager.ShowEventEffects)
            {
                if (influenceAmount != 0)
                {
                    ecbParallelWriter.AddComponent(index, ecbParallelWriter.CreateEntity(index), new SocialEffect
                    {
                        Position = position,
                        Type = influenceAmount > 0
                            ? SocialEffectType.Positive
                            : SocialEffectType.Negative
                    });
                }
            }
        }

        private static bool InvalidationCausedUnitToLoseAccessToBeds(GridManager gridManager, Entity entity,
            int2 targetCell)
        {
            return gridManager.IsBed(targetCell) && gridManager.IsOccupied(targetCell, entity) &&
                   !gridManager.TryGetClosestBedSemiRandom(targetCell, out _, true);
        }
    }
}