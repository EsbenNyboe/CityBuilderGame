using Rendering;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;

[UpdateInGroup(typeof(UnitStateSystemGroup))]
[BurstCompile]
public partial struct PathFollowSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;
    private const bool ShowDebug = false;
    private const float AnnoyanceFromBedOccupant = 0.5f;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (localTransform, pathPositionBuffer, pathFollow, spriteTransform, socialRelationships, entity) in
                 SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>,
                         RefRW<SpriteTransform>, RefRW<SocialRelationships>>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                continue;
            }

            var pathPosition = pathPositionBuffer[pathFollow.ValueRO.PathIndex].Position;
            var targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
            var moveDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);
            var moveSpeed = 5f;

            var distanceBeforeMoving = math.distance(localTransform.ValueRO.Position, targetPosition);
            localTransform.ValueRW.Position +=
                moveDirection * moveSpeed * SystemAPI.Time.DeltaTime; // * Globals.GameSpeed();
            var distanceAfterMoving = math.distance(localTransform.ValueRO.Position, targetPosition);

            if (ShowDebug)
            {
                var pathEndPosition = pathPositionBuffer[0].Position;
                Debug.DrawLine(localTransform.ValueRO.Position, new Vector3(pathEndPosition.x, pathEndPosition.y),
                    Color.red);
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

                if (pathFollow.ValueRO.PathIndex < 0)
                {
                    localTransform.ValueRW.Position = targetPosition;

                    var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

                    if (!gridManager.IsOccupied(targetPosition, entity))
                    {
                        gridManager.SetOccupant(targetPosition, entity);
                        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
                    }
                    else
                    {
                        if (gridManager.IsBed(targetPosition) && gridManager.IsOccupied(targetPosition, entity))
                        {
                            gridManager.TryGetOccupant(targetPosition, out var occupant);
                            socialRelationships.ValueRW.Relationships[occupant] -= AnnoyanceFromBedOccupant;

                            ecb.AddComponent(ecb.CreateEntity(), new DeathAnimationEvent
                            {
                                Position = targetPosition
                            });
                            var cell = GridHelpers.GetXY(targetPosition);
                            gridManager.TryClearBed(cell);
                            gridManager.TryClearOccupant(cell, entity);
                            ecb.DestroyEntity(entity);
                        }

                        GridHelpers.GetXY(targetPosition, out var x, out var y);
                        if (gridManager.TryGetNearbyEmptyCellSemiRandom(new int2(x, y), out var vacantCell))
                        {
                            PathHelpers.TrySetPath(ecb, entity, new int2(x, y), vacantCell);
                        }
                        else
                        {
                            DebugHelper.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: ", entity);
                        }
                    }
                }
            }

            if (moveDirection.x != 0)
            {
                var angleInDegrees = moveDirection.x > 0 ? 0f : 180f;
                spriteTransform.ValueRW.Position = Vector3.zero;
                spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}