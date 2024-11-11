using UnitAgency;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ISystem = Unity.Entities.ISystem;
using SystemAPI = Unity.Entities.SystemAPI;
using SystemHandle = Unity.Entities.SystemHandle;
using SystemState = Unity.Entities.SystemState;

namespace UnitBehaviours.Sleeping
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [BurstCompile]
    public partial struct IsSeekingBedSystem : ISystem
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
            var currentTime = SystemAPI.Time.ElapsedTime;

            foreach (var (localTransform, pathFollow, isSeekingBed, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRO<IsSeekingBed>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                // I reacted my destination / I'm standing still: I should find a bed!
                var ecb = GetEntityCommandBuffer(ref state);
                var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);

                // Am I on a bed?
                if (gridManager.IsBed(currentCell) && !gridManager.IsOccupied(currentCell, entity))
                {
                    // Ahhhh, I found my bed! 
                    ecb.RemoveComponent<IsSeekingBed>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                jobHandleList.Add(new SeekBedJob
                {
                    CurrentCell = currentCell,
                    Entity = entity,
                    GridManager = gridManager,
                    ECB = ecb
                }.Schedule());
            }

            JobHandle.CompleteAll(jobHandleList.AsArray());
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

        public void Execute()
        {
            // I'm not on a bed... I should find the closest bed.
            if (!GridManager.TryGetClosestBedSemiRandom(CurrentCell, out var closestAvailableBed))
            {
                // There is no available bed anywhere!
                // Am I standing on a bed? 
                if (GridManager.IsInteractable(CurrentCell))
                {
                    // TODO: Check if this actually ever happens.
                    // Whoops, someone is sleeping here.. I should move..
                    if (GridManager.TryGetNearbyEmptyCellSemiRandom(CurrentCell, out var nearbyCell))
                    {
                        PathHelpers.TrySetPath(ECB, Entity, CurrentCell, nearbyCell);
                    }
                }

                // I guess I have to wait for a bed to be available...
                // Let me try again later..
                ECB.RemoveComponent<IsSeekingBed>(Entity);
                ECB.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I found a bed!! I will go there! 
            PathHelpers.TrySetPath(ECB, Entity, CurrentCell, closestAvailableBed);
        }
    }
}