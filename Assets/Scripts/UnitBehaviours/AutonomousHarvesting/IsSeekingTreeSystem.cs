using Debugging;
using Grid;
using SystemGroups;
using UnitAgency;
using UnitAgency.Data;
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
    public struct IsSeekingTree : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingTreeSystem : ISystem
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
            var isDebuggingSeek = debugToggleManager.DebugTreeSeeking;
            var isDebuggingPath = debugToggleManager.DebugPathfinding;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            foreach (var (localTransform, pathFollow, moodInitiative, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithAll<IsSeekingTree>()
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

                // Am I adjacent to a tree?
                if (gridManager.TryGetNeighbouringTreeCell(currentCell, out _))
                {
                    // I found my adjacent tree!
                    var ecb = GetEntityCommandBuffer(ref state);
                    ecb.RemoveComponent<IsSeekingTree>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                // I'm not next to a tree... I should find the closest tree.
                jobHandleList.Add(new SeekTreeJob
                {
                    CurrentCell = currentCell,
                    Entity = entity,
                    GridManager = gridManager,
                    Ecb = GetEntityCommandBuffer(ref state),
                    IsDebuggingSeek = isDebuggingSeek,
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
    public struct SeekTreeJob : IJob
    {
        [ReadOnly] public int2 CurrentCell;
        [ReadOnly] public Entity Entity;
        [ReadOnly] public GridManager GridManager;
        public EntityCommandBuffer Ecb;

        [ReadOnly] public bool IsDebuggingSeek;
        [ReadOnly] public bool IsDebuggingPath;

        public void Execute()
        {
            if (!GridManager.TryGetClosestChoppingCellSemiRandom(CurrentCell, Entity, out var choppingCell,
                    IsDebuggingSeek))
            {
                // I can't see any nearby trees
                Ecb.RemoveComponent<IsSeekingTree>(Entity);
                Ecb.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I found a tree!! I will go there! 
            PathHelpers.TrySetPath(Ecb, GridManager, Entity, CurrentCell, choppingCell, IsDebuggingPath);
        }
    }
}