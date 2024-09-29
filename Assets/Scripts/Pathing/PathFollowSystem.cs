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
        if (!GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).IsOccupied())
        {
            // Debug.Log("Set occupied: " + entity);
            GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).SetOccupied(entity);
            return;
        }
        // IS OCCUPIED:
        // Debug.Log("OCCUPIED: " + GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).GetOwner());

        var newX = -1;
        var newY = -1;

        if (EntityManager.IsComponentEnabled<HarvestingUnit>(entity))
        {
            // Debug.Log("Unit cannot harvest, because cell is occupied: " + posX + " " + posY);

            if (TryGetNearbyChoppingCell(entity, out newX, out newY))
            {
                SetupPathfinding(entityCommandBuffer, localTransform, entity, new int2(newX, newY));
                return;
            }

            Debug.LogWarning("Could not find nearby chopping cell. Disabling harvesting-behaviour...");
        }

        // var movePositionList = PathingHelpers.GetCellListAroundTargetCell(new int2(posX, posY), 20);
        var nearbyCells = PathingHelpers.GetCellListAroundTargetCell(posX, posY, 20);
        if (!TryGetNearbyVacantCell(nearbyCells.Item1, nearbyCells.Item2, out newX, out newY))
        {
            Debug.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: " + entity);
            return;
        }

        SetupPathfinding(entityCommandBuffer, localTransform, entity, new int2(newX, newY));
        DisableHarvestingUnit(entity);
    }

    private void SetHarvestingUnit(Entity entity, int2 newTarget)
    {
        // EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
        EntityManager.SetComponentData(entity, new HarvestingUnit
        {
            Target = newTarget
        });
    }

    private void DisableHarvestingUnit(Entity entity)
    {
        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
        EntityManager.SetComponentData(entity, new HarvestingUnit
        {
            Target = new int2(-1, -1)
        });
    }

    private bool TryGetNearbyChoppingCell(Entity entity, out int newPathTargetX, out int newPathTargetY)
    {
        var harvestTarget = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
        var (nearbyCellsX, nearbyCellsY) = PathingHelpers.GetCellListAroundTargetCell(harvestTarget.x, harvestTarget.y, 20);

        if (GridSetup.Instance.DamageableGrid.GetGridObject(harvestTarget.x, harvestTarget.y).IsDamageable() &&
            TryGetValidNeighbourCell(harvestTarget.x, harvestTarget.y, out newPathTargetX, out newPathTargetY))
        {
            return true;
        }

        var count = nearbyCellsX.Count;
        for (var i = 1; i < count; i++)
        {
            var x = nearbyCellsX[i];
            var y = nearbyCellsY[i];

            if (!PathingHelpers.IsPositionInsideGrid(x, y) ||
                !GridSetup.Instance.DamageableGrid.GetGridObject(x, y).IsDamageable())
            {
                continue;
            }

            if (TryGetValidNeighbourCell(x, y, out newPathTargetX, out newPathTargetY))
            {
                SetHarvestingUnit(entity, new int2(x, y));
                return true;
            }
        }

        newPathTargetX = -1;
        newPathTargetY = -1;
        return false;
    }

    private bool TryGetValidNeighbourCell(int x, int y, out int neighbourX, out int neighbourY)
    {
        for (var j = 0; j < 8; j++)
        {
            PathingHelpers.GetNeighbourCell(j, x, y, out neighbourX, out neighbourY);

            if (PathingHelpers.IsPositionInsideGrid(neighbourX, neighbourY) &&
                PathingHelpers.IsPositionWalkable(neighbourX, neighbourY) &&
                !PathingHelpers.IsPositionOccupied(neighbourX, neighbourY))
            {
                return true;
            }
        }

        neighbourX = -1;
        neighbourY = -1;
        return false;
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

    private static bool TryGetNearbyVacantCell(List<int> cellsX, List<int> cellsY, out int x, out int y)
    {
        for (var i = 1; i < cellsX.Count; i++)
        {
            x = cellsX[i];
            y = cellsY[i];
            if (PathingHelpers.IsPositionInsideGrid(x, y) && PathingHelpers.IsPositionWalkable(x, y)
                                                          && !PathingHelpers.IsPositionOccupied(x, y)
               )
            {
                return true;
            }
        }

        x = default;
        y = default;
        return false;
    }
}