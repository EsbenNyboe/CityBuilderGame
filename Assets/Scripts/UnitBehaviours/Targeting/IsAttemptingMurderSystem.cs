using UnitAgency;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.Pathing
{
    public struct IsAttemptingMurder : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsAttemptingMurderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        private const float AttackRange = 0.5f;

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (targetFollow, entity) in SystemAPI.Query<RefRW<TargetFollow>>().WithEntityAccess()
                         .WithAll<IsAttemptingMurder>())
            {
                if (targetFollow.ValueRO.Target == Entity.Null)
                {
                    ecb.RemoveComponent<IsAttemptingMurder>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (targetFollow.ValueRO.CurrentDistanceToTarget < AttackRange)
                {
                    SystemAPI.SetComponentEnabled<IsAlive>(targetFollow.ValueRO.Target, false);
                    targetFollow.ValueRW.Target = Entity.Null;
                    targetFollow.ValueRW.CurrentDistanceToTarget = math.INFINITY;

                    ecb.RemoveComponent<IsAttemptingMurder>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }
    }
}