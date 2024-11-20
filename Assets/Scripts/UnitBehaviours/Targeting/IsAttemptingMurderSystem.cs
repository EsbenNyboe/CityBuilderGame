using UnitAgency;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

        public const float AttackRange = 1.5f;

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (targetFollow, pathFollow, localTransform, entity) in SystemAPI
                         .Query<RefRW<TargetFollow>, RefRO<PathFollow>, RefRO<LocalTransform>>()
                         .WithEntityAccess()
                         .WithAll<IsAttemptingMurder>())
            {
                if (targetFollow.ValueRO.Target == Entity.Null)
                {
                    ecb.RemoveComponent<IsAttemptingMurder>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (targetFollow.ValueRO.CurrentDistanceToTarget < AttackRange)
                {
                    SystemAPI.SetComponentEnabled<IsAlive>(targetFollow.ValueRO.Target, false);
                    targetFollow.ValueRW.Target = Entity.Null;
                    targetFollow.ValueRW.CurrentDistanceToTarget = math.INFINITY;
                    targetFollow.ValueRW.DesiredRange = -1;

                    ecb.RemoveComponent<IsAttemptingMurder>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
                else
                {
                    Debug.LogError("I can't murder this one...");
                }
            }
        }
    }
}