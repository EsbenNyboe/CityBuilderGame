using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public struct IsMurdering : IComponentData
    {
        public Entity Target;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsMurderingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (isMurdering, attackAnimation, entity) in
                     SystemAPI.Query<RefRO<IsMurdering>, RefRW<AttackAnimation>>()
                         .WithEntityAccess())
            {
                var target = isMurdering.ValueRO.Target;
                if (target == Entity.Null)
                {
                    // My target is already dead.
                    RemoveBehaviour(ecb, entity, attackAnimation);
                    continue;
                }

                if (attackAnimation.ValueRO.Target.x < 0)
                {
                    // I will start attacking now!
                    var targetPosition = SystemAPI.GetComponentLookup<LocalTransform>()[target].Position;
                    SystemAPI.SetComponent(entity, new AttackAnimation(targetPosition));
                    continue;
                }

                if (attackAnimation.ValueRO.TimeLeft <= 0)
                {
                    // I can now kill my target!
                    SystemAPI.SetComponentEnabled<IsAlive>(target, false);
                    attackAnimation.ValueRW.TimeLeft = attackAnimationManager.AttackDuration;
                    RemoveBehaviour(ecb, entity, attackAnimation);
                }
            }
        }

        private static void RemoveBehaviour(EntityCommandBuffer ecb, Entity entity,
            RefRW<AttackAnimation> attackAnimation)
        {
            ecb.RemoveComponent<IsMurdering>(entity);
            attackAnimation.ValueRW.MarkedForDeletion = true;
            ecb.AddComponent<IsDeciding>(entity);
        }
    }
}