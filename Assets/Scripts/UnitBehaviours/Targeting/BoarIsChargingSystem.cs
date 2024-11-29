using Audio;
using UnitAgency;
using UnitBehaviours.Pathing;
using UnitSpawn;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public struct BoarIsCharging : IComponentData
    {
    }

    public partial struct BoarIsChargingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (boarIsCharging, actionGate, localTransform, entity) in SystemAPI
                         .Query<RefRO<BoarIsCharging>, RefRO<ActionGate>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (SystemAPI.Time.ElapsedTime < actionGate.ValueRO.MinTimeOfAction)
                {
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                if (!QuadrantSystem.TryFindClosestEntity(quadrantDataManager.QuadrantMultiHashMap,
                        QuadrantSystem.GetPositionHashMapKey(position),
                        gridManager.GetSection(cell), position, entity, out var closestTargetEntity, out var closestTargetDistance))
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