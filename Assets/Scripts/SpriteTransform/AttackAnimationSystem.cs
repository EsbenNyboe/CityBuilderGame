using CustomTimeCore;
using SystemGroups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SpriteTransformNS
{
    [BurstCompile]
    [UpdateAfter(typeof(AttackAnimationManagerSystem))]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial struct AttackAnimationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AttackAnimationManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();

            foreach (var (attackAnimation, spriteTransform, localTransform, entity) in SystemAPI
                         .Query<RefRW<AttackAnimation>, RefRW<SpriteTransform>,
                             RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (DoAttackAnimation(ref state, timeScale,
                        spriteTransform,
                        attackAnimation,
                        localTransform.ValueRO.Position,
                        attackAnimationManager.AttackDuration,
                        attackAnimationManager.AttackAnimationSize,
                        attackAnimationManager.AttackAnimationIdleTime,
                        out var isIdling) &&
                    !isIdling)
                {
                    continue;
                }

                if (!attackAnimation.ValueRO.MarkedForDeletion)
                {
                    continue;
                }

                spriteTransform.ValueRW.Position = 0;
                // spriteTransform.ValueRW.Rotation = quaternion.identity;
                ecb.RemoveComponent<AttackAnimation>(entity);
            }
        }

        private bool DoAttackAnimation(ref SystemState state, float timeScale,
            RefRW<SpriteTransform> spriteTransform,
            RefRW<AttackAnimation> attackAnimation,
            float3 localTransformPosition,
            float duration, float size, float idleTime, out bool isIdling)
        {
            // Manage animation state:
            attackAnimation.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime * timeScale;
            var timeLeft = attackAnimation.ValueRO.TimeLeft;
            if (timeLeft <= 0)
            {
                isIdling = true;
                return false;
            }

            // Calculate animation input:
            var timeLeftNormalized = timeLeft / duration;
            var timeLeftBeforeIdling = timeLeftNormalized - idleTime;
            var timeLeftBeforeIdlingNormalized = math.max(0, timeLeftBeforeIdling) * (1 + idleTime);

            // Calculate animation output:
            var positionDistanceFromOrigin = timeLeftBeforeIdlingNormalized * size;

            var targetCell = attackAnimation.ValueRO.Target;
            var targetPosition = new float3(targetCell.x, targetCell.y, 0);
            var attackDirection = ((Vector3)(targetPosition - localTransformPosition)).normalized;

            var spritePositionOffset = positionDistanceFromOrigin * attackDirection;
            var angleInDegrees = spritePositionOffset.x > 0 ? 0f : 180f;
            var spriteRotationOffset = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);

            // Apply animation output:
            spriteTransform.ValueRW.Position = spritePositionOffset;
            if (spritePositionOffset.x != 0)
            {
                spriteTransform.ValueRW.Rotation = spriteRotationOffset;
            }

            isIdling = timeLeftBeforeIdlingNormalized == 0;
            return true;
        }
    }
}