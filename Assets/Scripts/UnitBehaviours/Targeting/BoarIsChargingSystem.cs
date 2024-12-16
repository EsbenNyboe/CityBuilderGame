using Audio;
using Grid;
using UnitAgency.Data;
using UnitBehaviours.ActionGateNS;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public struct BoarIsCharging : IComponentData
    {
    }

    public partial struct BoarIsChargingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var quadrantsToSearch = GridHelpers.CalculatePositionListLength(unitBehaviourManager.BoarQuadrantRange);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (boarIsCharging, actionGate, localTransform, entity) in SystemAPI
                         .Query<RefRO<BoarIsCharging>, RefRO<ActionGate>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (SystemAPI.Time.ElapsedTime < actionGate.ValueRO.MinTimeOfAction)
                {
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                if (!QuadrantSystem.TryFindClosestEntity(quadrantDataManager.VillagerQuadrantMap, gridManager, quadrantsToSearch,
                        position, entity, out var closestTargetEntity, out var closestTargetDistance))
                {
                    ecb.RemoveComponent<BoarIsCharging>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                ecb.RemoveComponent<BoarIsCharging>(entity);
                ecb.AddComponent(entity, new IsAttemptingMurder());
                ecb.SetComponent(entity, new TargetFollow
                {
                    Target = closestTargetEntity,
                    CurrentDistanceToTarget = closestTargetDistance,
                    DesiredRange = IsAttemptingMurderSystem.AttackRange
                });
                ecb.AddComponent(ecb.CreateEntity(), new SoundEvent
                {
                    Position = position,
                    Type = SoundEventType.BoarCharge
                });
            }
        }
    }
}