﻿using Debugging;
using UnitAgency;
using UnitState;
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
    [BurstCompile]
    public partial struct IsSeekingTreeSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var isDebuggingSeek = debugToggleManager.DebugTreeSeeking;
            var isDebuggingPath = debugToggleManager.DebugPathfinding;

            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
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

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                // I reacted my destination / I'm standing still: I should find a tree!
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
            // Am I adjacent to a tree?
            if (GridManager.TryGetNeighbouringTreeCell(CurrentCell, out _))
            {
                // I found my adjacent tree! 
                Ecb.RemoveComponent<IsSeekingTree>(Entity);
                Ecb.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I'm not next to a tree... I should find the closest tree.
            if (!GridManager.TryGetClosestChoppingCellSemiRandom(CurrentCell, Entity, out var choppingCell,
                    IsDebuggingSeek))
            {
                // I can't see any nearby trees
                Ecb.RemoveComponent<IsSeekingTree>(Entity);
                Ecb.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I found a tree!! I will go there! 
            PathHelpers.TrySetPath(Ecb, Entity, CurrentCell, choppingCell, IsDebuggingPath);
        }
    }
}