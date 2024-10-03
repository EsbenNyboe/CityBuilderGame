using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class PathFollowSystem : SystemBase
{
    private const bool ShowDebug = true;

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

            if (ShowDebug)
            {
                var pathEndPosition = pathPositionBuffer[0].Position;
                Debug.DrawLine(localTransform.ValueRO.Position, new Vector3(pathEndPosition.x, pathEndPosition.y), Color.red);
            }

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.1f)
            {
                // next waypoint
                pathFollow.ValueRW.PathIndex --;

                if (pathFollow.ValueRO.PathIndex < 0)
                {
                    localTransform.ValueRW.Position = targetPosition;

                    if (EntityManager.IsComponentEnabled<DeliveringUnit>(entity))
                    {
                        // if (!EntityManager.HasComponent<PathfindingParams>(entity))
                        // {
                        // }
                        var harvestTarget = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
                        SetupPathfinding(entityCommandBuffer, localTransform, entity, harvestTarget);
                        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
                        EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);

                        var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
                        if (occupationCell.EntityIsOwner(entity))
                        {
                            occupationCell.SetOccupied(Entity.Null);
                        }
                    }
                    else
                    {
                        HandleCellOccupation(entityCommandBuffer, localTransform, entity);
                    }
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

            var harvestTarget = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
            if (PathingHelpers.TryGetNearbyChoppingCell(harvestTarget, out var newTarget, out var newPathTarget))
            {
                SetHarvestingUnit(entity, newTarget);
                SetupPathfinding(entityCommandBuffer, localTransform, entity, newPathTarget);
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

        var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
        if (occupationCell.EntityIsOwner(entity))
        {
            occupationCell.SetOccupied(Entity.Null);
        }
    }

    private void SetHarvestingUnit(Entity entity, int2 newTarget)
    {
        // EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
        EntityManager.SetComponentData(entity, new HarvestingUnit
        {
            Target = newTarget
        });
    }

    // TODO: Fix race-condition with DeliveringUnitSystem
    private void DisableHarvestingUnit(Entity entity)
    {
        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
        //EntityManager.SetComponentData(entity, new HarvestingUnit
        //{
        //    Target = new int2(-1, -1)
        //});

        EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);
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