using Debugging;
using Grid;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace UnitBehaviours.Sleeping
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingBedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var isDebuggingPath = debugToggleManager.DebugPathfinding;
            var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();

            foreach (var (isSeekingBed, localTransform, pathFollow, moodInitiative, entity) in SystemAPI
                         .Query<RefRW<IsSeekingBed>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);

                // Am I on a bed?
                if (gridManager.IsBedAvailableToUnit(cell, entity))
                {
                    // Ahhhh, I found my bed!
                    ecb.RemoveComponent<IsSeekingBed>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                // I'm not on a bed... I will seek the closest bed.
                if (!QuadrantSystem.TryFindClosestAvailableGridEntity(quadrantDataManager.BedQuadrantMap, gridManager,
                        unitBehaviourManager.QuadrantSearchRange,
                        position, entity, out var closestTargetEntity, out _))
                {
                    // There is no available bed anywhere!
                    if (gridManager.IsInteractable(cell))
                    {
                        // Whoops, I'm standing on a bed.. I should move..
                        if (gridManager.TryGetNearbyEmptyCellSemiRandom(cell, out var nearbyCell, isDebuggingSearch))
                        {
                            PathHelpers.TrySetPath(ecb, gridManager, entity, cell, nearbyCell, isDebuggingPath);
                        }
                    }

                    // I guess I have to wait for a bed to be available...
                    // I'll keep checking all beds every frame, until I succeed!!!!
                    continue;
                }

                // I found a bed!! I will go there! 
                var closestBedCell = GridHelpers.GetXY(SystemAPI.GetComponent<LocalTransform>(closestTargetEntity).Position);
                PathHelpers.TrySetPath(ecb, gridManager, entity, cell, closestBedCell, isDebuggingPath);
            }

            state.Dependency = JobHandle.CombineDependencies(jobHandleList.AsArray());
            jobHandleList.Dispose();
        }
    }
}