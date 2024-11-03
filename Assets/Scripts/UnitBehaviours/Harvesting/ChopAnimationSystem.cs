using UnitBehaviours.AutonomousHarvesting;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct ChopAnimationTag : IComponentData
{
}

[BurstCompile]
public partial struct ChopAnimationSystem : ISystem
{
    private SystemHandle _chopAnimationManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _chopAnimationManagerSystemHandle = state.World.GetExistingSystem<ChopAnimationManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var chopAnimationManager = SystemAPI.GetComponent<ChopAnimationManager>(_chopAnimationManagerSystemHandle);
        var chopDuration = chopAnimationManager.ChopDuration;
        var chopSize = chopAnimationManager.ChopAnimationSize;
        var chopIdleTime = chopAnimationManager.ChopAnimationIdleTime;

        foreach (var (isHarvesting, spriteTransform, localTransform) in SystemAPI
                     .Query<RefRO<IsHarvesting>, RefRW<SpriteTransform>, RefRO<LocalTransform>>().WithPresent<ChopAnimationTag>())
        {
            DoChopAnimation(ref state, isHarvesting, spriteTransform, localTransform, chopDuration, chopSize, chopIdleTime);
        }
    }

    private void DoChopAnimation(ref SystemState state, RefRO<IsHarvesting> isHarvesting, RefRW<SpriteTransform> spriteTransform,
        RefRO<LocalTransform> localTransform, float chopDuration, float chopSize, float chopIdleTime)
    {
        // Manage animation state:
        var timeLeft = isHarvesting.ValueRO.TimeUntilNextChop;

        if (timeLeft < 0)
        {
            // Now this animation is doing nothing... Someone else will have to clean up this mess, and remove this component! 
            return;
        }

        // Calculate animation input:
        var timeLeftNormalized = timeLeft / chopDuration;
        var timeLeftBeforeIdling = timeLeftNormalized - chopIdleTime;
        var timeLeftBeforeIdlingNormalized = math.max(0, timeLeftBeforeIdling) * (1 + chopIdleTime);

        // Calculate animation output:
        var positionDistanceFromOrigin = timeLeftBeforeIdlingNormalized * chopSize;

        var chopTargetCell = isHarvesting.ValueRO.Tree;
        var chopTargetPosition = new float3(chopTargetCell.x, chopTargetCell.y, 0);
        var chopDirection = ((Vector3)(chopTargetPosition - localTransform.ValueRO.Position)).normalized;

        var spritePositionOffset = positionDistanceFromOrigin * chopDirection;
        var angleInDegrees = 0f; // TODO: Set animation-direction here?
        var spriteRotationOffset = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);

        // Apply animation output:
        spriteTransform.ValueRW.Position = spritePositionOffset;
        spriteTransform.ValueRW.Rotation = spriteRotationOffset;
    }
}