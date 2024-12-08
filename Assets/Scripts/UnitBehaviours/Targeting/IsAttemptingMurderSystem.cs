using UnitAgency;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Pathing
{
    public struct IsAttemptingMurder : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsAttemptingMurderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public const float AttackRange = 1.5f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (targetFollow, pathFollow, localTransform, entity) in SystemAPI
                         .Query<RefRW<TargetFollow>, RefRW<PathFollow>, RefRO<LocalTransform>>()
                         .WithEntityAccess()
                         .WithAll<IsAttemptingMurder>())
            {
                pathFollow.ValueRW.MoveSpeedMultiplier = unitBehaviourManager.MoveSpeedWhenAttemptingMurder;

                if (targetFollow.ValueRO.Target == Entity.Null)
                {
                    RemoveBehaviour(ecb, entity, pathFollow);
                    continue;
                }

                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (targetFollow.ValueRO.CurrentDistanceToTarget > targetFollow.ValueRO.DesiredRange)
                {
                    // Target is out of reach. I'll wait a bit for TargetFollow to do its thing.
                    continue;
                }

                RemoveTarget(targetFollow);
                RemoveBehaviour(ecb, entity, pathFollow);
            }
        }

        private static void RemoveTarget(RefRW<TargetFollow> targetFollow)
        {
            targetFollow.ValueRW.Target = Entity.Null;
            targetFollow.ValueRW.CurrentDistanceToTarget = math.INFINITY;
            targetFollow.ValueRW.DesiredRange = -1;
        }

        private static void RemoveBehaviour(EntityCommandBuffer ecb, Entity entity, RefRW<PathFollow> pathFollow)
        {
            pathFollow.ValueRW.MoveSpeedMultiplier = 1;
            ecb.RemoveComponent<IsAttemptingMurder>(entity);
            ecb.AddComponent<IsDeciding>(entity);
        }
    }
}