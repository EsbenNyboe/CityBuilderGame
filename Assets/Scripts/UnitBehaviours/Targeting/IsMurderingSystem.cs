using Events;
using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public struct Health : IComponentData
    {
        public float CurrentHealth;
        public float MaxHealth;
    }

    public struct IsMurdering : IComponentData
    {
        public Entity Target;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsMurderingSystem : ISystem
    {
        private const float MaxRange = 2f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (isMurdering, attackAnimation, localTransform, entity) in
                     SystemAPI.Query<RefRO<IsMurdering>, RefRW<AttackAnimation>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var target = isMurdering.ValueRO.Target;
                if (target == Entity.Null)
                {
                    // My target is already dead.
                    RemoveBehaviour(ecb, entity, attackAnimation);
                    continue;
                }

                var targetPosition = SystemAPI.GetComponentLookup<LocalTransform>()[target].Position;
                var targetPositionOffset = SystemAPI.GetComponentLookup<SpriteTransform>()[target].Position;
                attackAnimation.ValueRW.Target = new float2(
                    targetPosition.x + targetPositionOffset.x,
                    targetPosition.y + targetPositionOffset.y);

                if (attackAnimation.ValueRO.TimeLeft <= 0)
                {
                    if (math.distance(localTransform.ValueRO.Position, targetPosition) > MaxRange)
                    {
                        // The target moved away! I cannot attack!! 
                        RemoveBehaviour(ecb, entity, attackAnimation);
                        continue;
                    }

                    // I can now attack my target!
                    attackAnimation.ValueRW.TimeLeft = attackAnimationManager.AttackDuration;
                    ecb.AddComponent(ecb.CreateEntity(), new DamageEvent
                    {
                        Position = targetPosition
                    });
                    var targetHealth = SystemAPI.GetComponentLookup<Health>()[target];
                    targetHealth.CurrentHealth -= unitBehaviourManager.DamagePerAttack;
                    SystemAPI.SetComponent(target, targetHealth);
                    if (targetHealth.CurrentHealth < 0)
                    {
                        // I will kill my target!
                        SystemAPI.SetComponentEnabled<IsAlive>(target, false);
                        RemoveBehaviour(ecb, entity, attackAnimation);
                    }
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