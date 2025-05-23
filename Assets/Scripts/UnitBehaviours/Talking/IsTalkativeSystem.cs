using CustomTimeCore;
using Grid;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Talking
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsTalkativeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var isTalkativeLookup = SystemAPI.GetComponentLookup<IsTalkative>();
            var isTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>();

            foreach (var (isTalkative, pathFollow, localTransform, entity) in SystemAPI
                         .Query<RefRW<IsTalkative>, RefRO<PathFollow>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                isTalkative.ValueRW.Patience -= SystemAPI.Time.DeltaTime * timeScale;
                if (isTalkative.ValueRW.Patience <= 0)
                {
                    // I lost my patience...
                    ecb.RemoveComponent<IsTalkative>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }

                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, isTalkativeLookup, out _) ||
                    TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, isTalkingLookup, out _))
                {
                    // I found someone to talk to!
                    ecb.RemoveComponent<IsTalkative>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }
    }
}