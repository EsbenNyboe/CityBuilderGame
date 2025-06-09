using Debugging;
using Grid;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Hunger;
using UnitBehaviours.Pathing;
using UnitState.Mood;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingBonfireSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var isDebuggingPath = debugToggleManager.DebugPathfinding;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            foreach (var (localTransform, pathFollow, moodInitiative, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithAll<IsSeekingBonfire>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (gridManager.IsOccupied(currentCell, entity))
                {
                    continue;
                }

                // Am I adjacent to a Bonfire?
                if (gridManager.TryGetNeighbouringBonfireCell(currentCell, out _))
                {
                    // I found my adjacent Bonfire!
                    var ecb = GetEntityCommandBuffer(ref state);
                    ecb.RemoveComponent<IsSeekingBonfire>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                // I'm not next to a Bonfire... I should find the closest Bonfire.
                jobHandleList.Add(new SeekBonfireJob
                {
                    CurrentCell = currentCell,
                    Entity = entity,
                    GridManager = gridManager,
                    Ecb = GetEntityCommandBuffer(ref state),
                    IsDebuggingSeek = false,
                    IsDebuggingPath = isDebuggingPath
                }.Schedule());
            }

            state.Dependency = JobHandle.CombineDependencies(jobHandleList.AsArray());
            jobHandleList.Dispose();
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecb.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }

    [BurstCompile]
    public struct SeekBonfireJob : IJob
    {
        [ReadOnly] public int2 CurrentCell;
        [ReadOnly] public Entity Entity;
        [ReadOnly] public GridManager GridManager;
        public EntityCommandBuffer Ecb;

        [ReadOnly] public bool IsDebuggingSeek;
        [ReadOnly] public bool IsDebuggingPath;

        public void Execute()
        {
            if (!GridManager.TryGetClosestBonfireSeatSemiRandom(CurrentCell, Entity, out var bonfireSeat,
                    IsDebuggingSeek))
            {
                // I can't see any nearby Bonfires
                Ecb.RemoveComponent<IsSeekingBonfire>(Entity);
                Ecb.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I found a Bonfire!! I will go there! 
            PathHelpers.TrySetPath(Ecb, GridManager, Entity, CurrentCell, bonfireSeat, IsDebuggingPath);
        }
    }
}