using UnitAgency;
using UnitState;
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
    public struct IsSeekingBed : IComponentData
    {
    }

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

            foreach (var (localTransform, pathFollow, moodInitiative, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithAll<IsSeekingBed>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                // I reacted my destination / I'm standing still: I should find a bed!
                jobHandleList.Add(new SeekBedJob
                {
                    CurrentCell = GridHelpers.GetXY(localTransform.ValueRO.Position),
                    Entity = entity,
                    GridManager = gridManager,
                    ECB = GetEntityCommandBuffer(ref state)
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

        public void Execute()
        {
            // Am I on a bed?
            if (GridManager.IsBed(CurrentCell) && !GridManager.IsOccupied(CurrentCell, Entity))
            {
                // Ahhhh, I found my bed! 
                ECB.RemoveComponent<IsSeekingBed>(Entity);
                ECB.AddComponent<IsDeciding>(Entity);
                return;
            }

            // I'm not on a bed... I should find the closest bed.
            if (!GridManager.TryGetClosestBedSemiRandom(CurrentCell, out var closestAvailableBed))
            {
                // There is no available bed anywhere!
                if (GridManager.IsInteractable(CurrentCell))
                {
                    // Whoops, I'm standing on a bed.. I should move..
                    if (GridManager.TryGetNearbyEmptyCellSemiRandom(CurrentCell, out var nearbyCell))
                    {
                        PathHelpers.TrySetPath(ECB, Entity, CurrentCell, nearbyCell);
                    }
                }

                // I guess I have to wait for a bed to be available...
                // I'll keep checking all beds every frame, until I succeed!!!!
                return;
            }

            // I found a bed!! I will go there! 
            PathHelpers.TrySetPath(ECB, Entity, CurrentCell, closestAvailableBed);
        }
    }
}