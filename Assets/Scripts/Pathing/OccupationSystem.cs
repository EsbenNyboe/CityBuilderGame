using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(PathFollowSystem))]
public partial class OccupationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        // TODO: Test if "WithAll" is necessary
        // TODO: Test if "WithAll<TryDeoccupy>" is faster that RefRO
        foreach (var (tryDeoccupy, localTransform, harvestingUnit, entity) in SystemAPI
                     .Query<RefRO<TryDeoccupy>, RefRO<LocalTransform>, RefRO<HarvestingUnit>>().WithAll<HarvestingUnit>().WithEntityAccess())
        {
            var newTarget = harvestingUnit.ValueRO.Target;
            HandleCellDeoccupation(entityCommandBuffer, localTransform, entity, newTarget);
            entityCommandBuffer.RemoveComponent<TryDeoccupy>(entity);
        }

        foreach (var (tryOccupy, localTransform, entity) in SystemAPI.Query<RefRO<TryOccupy>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            HandleCellOccupation(entityCommandBuffer, localTransform, entity);
            entityCommandBuffer.RemoveComponent<TryOccupy>(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void HandleCellDeoccupation(EntityCommandBuffer entityCommandBuffer, RefRO<LocalTransform> localTransform, Entity entity, int2 newTarget)
    {
        var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
        if (occupationCell.EntityIsOwner(entity))
        {
            occupationCell.SetOccupied(Entity.Null);
        }

        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
        EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);
        SetupPathfinding(entityCommandBuffer, localTransform, entity, newTarget);
    }

    private void HandleCellOccupation(EntityCommandBuffer entityCommandBuffer, RefRO<LocalTransform> localTransform,
        Entity entity)
    {
        GridSetup.Instance.OccupationGrid.GetXY(localTransform.ValueRO.Position, out var posX, out var posY);

        // TODO: Check if it's safe to assume the occupied cell owner is not this entity
        if (!GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).IsOccupied())
        {
            // Debug.Log("Set occupied: " + entity);
            GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).SetOccupied(entity);
            return;
        }

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

        var nearbyCells = PathingHelpers.GetCellListAroundTargetCell30Rings(posX, posY);
        //var nearbyCells = PathingHelpers.GetCellListAroundTargetCell(posX, posY, 20);
        if (!TryGetNearbyVacantCell(nearbyCells, out var vacantCell))
        {
            Debug.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: " + entity);
            return;
        }

        SetupPathfinding(entityCommandBuffer, localTransform, entity, vacantCell);
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

    private static bool TryGetNearbyVacantCell(int2[] movePositionList, out int2 nearbyCell)
    {
        for (var i = 1; i < movePositionList.Length; i++)
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

    private void SetupPathfinding(EntityCommandBuffer entityCommandBuffer, RefRO<LocalTransform> localTransform, Entity entity, int2 newEndPosition)
    {
        GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
        PathingHelpers.ValidateGridPosition(ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });
    }
}