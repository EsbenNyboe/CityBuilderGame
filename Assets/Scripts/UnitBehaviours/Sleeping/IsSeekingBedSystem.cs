using Debugging;
using Grid;
using SystemGroups;
using UnitAgency;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Sleeping
{
    public struct IsSeekingBed : IComponentData
    {
        public int Attempts;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingBedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
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
            var isDebuggingSeek = debugToggleManager.DebugBedSeeking;
            var isDebuggingPath = debugToggleManager.DebugPathfinding;
            var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
            var ecb = GetEntityCommandBuffer(ref state);

            foreach (var (isSeekingBed, localTransform, pathFollow, moodInitiative, entity) in SystemAPI
                         .Query<RefRW<IsSeekingBed>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);

                // Am I on a bed?
                if (gridManager.IsBedAvailableToUnit(currentCell, entity))
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

                if (isSeekingBed.ValueRO.Attempts > unitBehaviourManager.MaxSeekAttempts)
                {
                    // I'll take a break from this activity...
                    ecb.RemoveComponent<IsSeekingBed>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                isSeekingBed.ValueRW.Attempts++;

                // I'm not on a bed... I will seek the closest bed.
                jobHandleList.Add(new SeekBedJob
                {
                    CurrentCell = currentCell,
                    Entity = entity,
                    GridManager = gridManager,
                    ECB = GetEntityCommandBuffer(ref state),
                    IsDebuggingSeek = isDebuggingSeek,
                    IsDebuggingSearch = isDebuggingSearch,
                    IsDebuggingPath = isDebuggingPath
                }.Schedule());
            }

            state.Dependency = JobHandle.CombineDependencies(jobHandleList.AsArray());
            jobHandleList.Dispose();
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }

    [BurstCompile]
    public struct SeekBedJob : IJob
    {
        [ReadOnly] public int2 CurrentCell;
        [ReadOnly] public Entity Entity;
        [ReadOnly] public GridManager GridManager;
        public EntityCommandBuffer ECB;

        [ReadOnly] public bool IsDebuggingSeek;
        [ReadOnly] public bool IsDebuggingSearch;
        [ReadOnly] public bool IsDebuggingPath;

        public void Execute()
        {
            if (!GridManager.TryGetClosestBedSemiRandom(CurrentCell, out var closestAvailableBed, IsDebuggingSeek))
            {
                // There is no available bed anywhere!
                if (GridManager.IsInteractable(CurrentCell))
                {
                    // Whoops, I'm standing on a bed.. I should move..
                    if (GridManager.TryGetNearbyEmptyCellSemiRandom(CurrentCell, out var nearbyCell, IsDebuggingSearch))
                    {
                        PathHelpers.TrySetPath(ECB, GridManager, Entity, CurrentCell, nearbyCell, IsDebuggingPath);
                    }
                }

                // I guess I have to wait for a bed to be available...
                // I'll keep checking all beds every frame, until I succeed!!!!
                return;
            }

            // I found a bed!! I will go there! 
            PathHelpers.TrySetPath(ECB, GridManager, Entity, CurrentCell, closestAvailableBed, IsDebuggingPath);
        }
    }
}