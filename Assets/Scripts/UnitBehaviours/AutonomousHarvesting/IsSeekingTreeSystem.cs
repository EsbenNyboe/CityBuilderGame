﻿using UnitAgency;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [BurstCompile]
    public partial struct IsSeekingTreeSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            foreach (var (localTransform, pathFollow, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>>().WithAll<IsSeekingTree>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }
                // I reacted my destination / I'm standing still: I should find a tree!

                var job = new SeekTreeJob
                {
                    CurrentCell = GridHelpers.GetXY(localTransform.ValueRO.Position),
                    Entity = entity,
                    GridManager = gridManager,
                    Ecb = GetEntityCommandBuffer(ref state)
                };
                jobHandleList.Add(job.Schedule());
            }

            JobHandle.CompleteAll(jobHandleList.AsArray());

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
            jobHandleList.Dispose();
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbTest = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecbTest.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }

    [BurstCompile]
    public struct SeekTreeJob : IJob
    {
        [ReadOnly] public int2 CurrentCell;
        [ReadOnly] public Entity Entity;
        [ReadOnly] public GridManager GridManager;
        public EntityCommandBuffer Ecb;

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
            if (!GridManager.TryGetClosestChoppingCellSemiRandom(CurrentCell, Entity, out var choppingCell))
            {
                // I can't see any nearby trees
                Ecb.RemoveComponent<IsSeekingTree>(Entity);
                Ecb.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I found a tree!! I will go there! 
            PathHelpers.TrySetPath(Ecb, Entity, CurrentCell, choppingCell);
        }
    }
}