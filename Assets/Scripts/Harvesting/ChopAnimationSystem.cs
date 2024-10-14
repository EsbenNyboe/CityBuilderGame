using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct ChopAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (chopAnimation, spriteTransform, localTransform) in SystemAPI
                     .Query<RefRW<ChopAnimation>, RefRW<SpriteTransform>, RefRO<LocalTransform>>())
        {
            DoChopAnimation(ref state, chopAnimation, spriteTransform, localTransform);
        }
    }

    private void DoChopAnimation(ref SystemState state, RefRW<ChopAnimation> chopAnimation, RefRW<SpriteTransform> spriteTransform,
        RefRO<LocalTransform> localTransform)
    {
        // Manage animation state:
        var timeLeft = chopAnimation.ValueRW.ChopAnimationProgress -= state.WorldUnmanaged.Time.DeltaTime;

        if (timeLeft < 0)
        {
            // Now this animation is doing nothing... Someone else will have to clean up this mess, and remove this component! 
            return;
        }

        // Calculate animation input:
        var timeLeftNormalized = timeLeft / chopAnimation.ValueRO.ChopDuration;
        var timeLeftBeforeIdling = timeLeftNormalized - chopAnimation.ValueRO.ChopIdleTime;
        var timeLeftBeforeIdlingNormalized = math.max(0, timeLeftBeforeIdling) * (1 + chopAnimation.ValueRO.ChopIdleTime);

        // Calculate animation output:
        var positionDistanceFromOrigin = timeLeftBeforeIdlingNormalized * chopAnimation.ValueRO.ChopSize;

        var chopTargetPosition = chopAnimation.ValueRO.TargetPosition;
        var chopDirection = ((Vector3)(chopTargetPosition - localTransform.ValueRO.Position)).normalized;

        var spritePositionOffset = positionDistanceFromOrigin * chopDirection;
        var angleInDegrees = 0f; // TODO: Set animation-direction here?
        var spriteRotationOffset = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);

        // Apply animation output:
        spriteTransform.ValueRW.Position = spritePositionOffset;
        spriteTransform.ValueRW.Rotation = spriteRotationOffset;
    }
}