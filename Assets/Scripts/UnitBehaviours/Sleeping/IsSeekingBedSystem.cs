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

            foreach (var (localTransform, pathFollow, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<PathFollow>>()
                         .WithAll<IsSeekingBed>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                // I reacted my destination / I'm standing still: I should find a bed!
                jobHandleList.Add(new SeekBedJob
                {
                    currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position),
                    entity = entity,
                    gridManager = gridManager,
                    ecb = GetEntityCommandBuffer(ref state)
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
        [ReadOnly] public int2 currentCell;
        [ReadOnly] public Entity entity;
        [ReadOnly] public GridManager gridManager;
        public EntityCommandBuffer ecb;

        public void Execute()
        {
            // Am I on a bed?
            if (gridManager.IsBed(currentCell) && !gridManager.IsOccupied(currentCell, entity))
            {
                // Ahhhh, I found my bed! 
                ecb.RemoveComponent<IsSeekingBed>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                return;
            }

            // I'm not on a bed... I should find the closest bed.
            if (!gridManager.TryGetClosestBedSemiRandom(currentCell, out var closestAvailableBed))
            {
                // There is no available bed anywhere!
                if (gridManager.IsInteractable(currentCell))
                {
                    // Whoops, I'm standing on a bed.. I should move..
                    if (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out var nearbyCell))
                    {
                        PathHelpers.TrySetPath(ecb, entity, currentCell, nearbyCell);
                    }
                }

                // I guess I have to wait for a bed to be available...
                // I'll keep checking all beds every frame, until I succeed!!!!
                return;
            }

            // I found a bed!! I will go there! 
            PathHelpers.TrySetPath(ecb, entity, currentCell, closestAvailableBed);
        }
    }
}