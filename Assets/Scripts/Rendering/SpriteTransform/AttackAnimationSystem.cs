using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct AttackAnimation : IComponentData
{
    public readonly float2 Target;
    public float TimeLeft;
    public bool MarkedForDeletion;

    public AttackAnimation(int2 target, float timeLeft = 0)
    {
        Target = target;
        TimeLeft = timeLeft;
        MarkedForDeletion = false;
    }

    public AttackAnimation(float3 target, float timeLeft = 0)
    {
        Target = new float2(target.x, target.y);
        TimeLeft = timeLeft;
        MarkedForDeletion = false;
    }
}

[BurstCompile]
[UpdateAfter(typeof(AttackAnimationManagerSystem))]
[UpdateInGroup(typeof(AnimationSystemGroup))]
public partial struct AttackAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<AttackAnimationManager>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();

        foreach (var (attackAnimation, spriteTransform, localTransform, entity) in SystemAPI
                     .Query<RefRW<AttackAnimation>, RefRW<SpriteTransform>,
                         RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (DoAttackAnimation(ref state,
                    spriteTransform,
                    attackAnimation,
                    localTransform.ValueRO.Position,
                    attackAnimationManager.AttackDuration,
                    attackAnimationManager.AttackAnimationSize,
                    attackAnimationManager.AttackAnimationIdleTime))
            {
                continue;
            }

            if (!attackAnimation.ValueRO.MarkedForDeletion)
            {
                continue;
            }

            spriteTransform.ValueRW.Position = 0;
            spriteTransform.ValueRW.Rotation = quaternion.identity;
            ecb.RemoveComponent<AttackAnimation>(entity);
        }
    }

    private bool DoAttackAnimation(ref SystemState state,
        RefRW<SpriteTransform> spriteTransform,
        RefRW<AttackAnimation> attackAnimation,
        float3 localTransformPosition,
        float duration, float size, float idleTime)
    {
        // Manage animation state:
        attackAnimation.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;
        var timeLeft = attackAnimation.ValueRO.TimeLeft;
        if (timeLeft <= 0)
        {
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
        var angleInDegrees = 0f; // TODO: Set animation-direction here?
        var spriteRotationOffset = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);

        // Apply animation output:
        spriteTransform.ValueRW.Position = spritePositionOffset;
        spriteTransform.ValueRW.Rotation = spriteRotationOffset;
        return true;
    }
}