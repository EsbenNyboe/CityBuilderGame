using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (localTransform, pathPositionBuffer, pathFollow, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                continue;
            }

            var pathPosition = pathPositionBuffer[pathFollow.ValueRO.PathIndex].Position;
            var targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
            var moveDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);
            var moveSpeed = 5f;

            localTransform.ValueRW.Position += moveDirection * moveSpeed * SystemAPI.Time.DeltaTime;

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.1f)
            {
                // next waypoint
                pathFollow.ValueRW.PathIndex --;

                if (pathFollow.ValueRO.PathIndex < 0)
                {
                    localTransform.ValueRW.Position = targetPosition;

                    HandleCellOccupation(entityCommandBuffer, localTransform, entity);
                }
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void HandleCellOccupation(EntityCommandBuffer entityCommandBuffer, RefRW<LocalTransform> localTransform, Entity entity)
    {
        GridSetup.Instance.OccupationGrid.GetXY(localTransform.ValueRO.Position, out var posX, out var posY);

        // TODO: Check if it's safe to assume the occupied cell owner is not this entity
        if (GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).IsOccupied())
        {
            Debug.Log("OCCUPIED: " + GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).GetOwner());
            var movePositionList = PathingHelpers.GetCellListAroundTargetCell(new int2(posX, posY), 20);

            if (!TryGetNearbyVacantCell(movePositionList, out var newEndPosition))
            {
                Debug.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: " + entity);
                return;
            }

            SetupPathfinding(entityCommandBuffer, localTransform, entity, newEndPosition);
        }
        else
        {
            Debug.Log("Set occupied: " + entity);
            GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).SetOccupied(entity);
        }
    }

    private void SetupPathfinding(EntityCommandBuffer entityCommandBuffer, RefRW<LocalTransform> localTransform, Entity entity, int2 newEndPosition)
    {
        GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
        PathingHelpers.ValidateGridPosition(ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });

        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
        EntityManager.SetComponentData(entity, new HarvestingUnit
        {
            Target = new int2(-1, -1)
        });
    }

    private static bool TryGetNearbyVacantCell(List<int2> movePositionList, out int2 nearbyCell)
    {
        for (var i = 1; i < movePositionList.Count; i++)
        {
            nearbyCell = movePositionList[i];
            if (PathingHelpers.IsPositionInsideGrid(nearbyCell) && PathingHelpers.IsPositionWalkable(nearbyCell)
                                                                && !PathingHelpers.IsPositionOccupied(nearbyCell)
               )
            {
                return true;
            }
        }

        nearbyCell = default;
        return false;
    }
}