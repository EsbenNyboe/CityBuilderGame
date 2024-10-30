using UnitAgency;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// [BurstCompile]
public partial struct PathFollowSystem : ISystem
{
    private const bool ShowDebug = true;

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (localTransform, pathPositionBuffer, pathFollow, spriteTransform, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>, RefRW<SpriteTransform>>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                continue;
            }

            var pathPosition = pathPositionBuffer[pathFollow.ValueRO.PathIndex].Position;
            Debug.Log("PathPosition: " + pathPosition);
            var targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
            var moveDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);
            var moveSpeed = 5f;

            var distanceBeforeMoving = math.distance(localTransform.ValueRO.Position, targetPosition);
            localTransform.ValueRW.Position += moveDirection * moveSpeed * SystemAPI.Time.DeltaTime; // * Globals.GameSpeed();
            var distanceAfterMoving = math.distance(localTransform.ValueRO.Position, targetPosition);

            if (ShowDebug)
            {
                var pathEndPosition = pathPositionBuffer[0].Position;
                Debug.DrawLine(localTransform.ValueRO.Position, new Vector3(pathEndPosition.x, pathEndPosition.y), Color.red);
            }

            var unitIsOnNextPathPosition = distanceAfterMoving < 0.1f;


            // HACK:
            if (!unitIsOnNextPathPosition && distanceAfterMoving > distanceBeforeMoving)
            {
                unitIsOnNextPathPosition = true;
                localTransform.ValueRW.Position = targetPosition;
            }

            if (unitIsOnNextPathPosition)
            {
                // next waypoint
                pathFollow.ValueRW.PathIndex--;
                Debug.Log("Move to: " + localTransform.ValueRO.Position);

                if (pathFollow.ValueRO.PathIndex < 0)
                {
                    localTransform.ValueRW.Position = targetPosition;

                    entityCommandBuffer.AddComponent<IsDecidingTag>(entity);
                }
            }


            if (moveDirection.x != 0)
            {
                var angleInDegrees = moveDirection.x > 0 ? 0f : 180f;
                spriteTransform.ValueRW.Position = Vector3.zero;
                spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);
            }
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}