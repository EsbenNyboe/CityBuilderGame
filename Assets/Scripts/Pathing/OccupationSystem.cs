using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(PathFollowSystem))]
[UpdateAfter(typeof(GridManagerSystem))]
public partial class OccupationSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        // TODO: Test if "WithAll" is necessary
        // TODO: Test if "WithAll<TryDeoccupy>" is faster that RefRO
        foreach (var (tryDeoccupy, localTransform, entity) in SystemAPI
                     .Query<RefRO<TryDeoccupy>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            var newTarget = tryDeoccupy.ValueRO.NewTarget;
            HandleCellDeoccupation(entityCommandBuffer, gridManager, localTransform, entity, newTarget);
            entityCommandBuffer.RemoveComponent<TryDeoccupy>(entity);
        }

        foreach (var (tryOccupy, localTransform, entity) in SystemAPI.Query<RefRO<TryOccupy>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            HandleCellOccupation(entityCommandBuffer, gridManager, localTransform, entity);
            entityCommandBuffer.RemoveComponent<TryOccupy>(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void HandleCellDeoccupation(EntityCommandBuffer entityCommandBuffer, GridManager gridManager, RefRO<LocalTransform> localTransform,
        Entity entity, int2 newTarget)
    {
        var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
        if (occupationCell.EntityIsOwner(entity))
        {
            occupationCell.SetOccupied(Entity.Null);
        }

        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
        EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);
        SetupPathfinding(gridManager, entityCommandBuffer, localTransform, entity, newTarget);
    }

    private void HandleCellOccupation(EntityCommandBuffer entityCommandBuffer, GridManager gridManager, RefRO<LocalTransform> localTransform,
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
            if (GridHelpers.TryGetNearbyChoppingCell(gridManager, harvestTarget, out var newTarget, out var newPathTarget))
            {
                SetHarvestingUnit(entity, newTarget);
                SetupPathfinding(gridManager, entityCommandBuffer, localTransform, entity, newPathTarget);
                return;
            }

            Debug.LogWarning("Could not find nearby chopping cell. Disabling harvesting-behaviour...");
        }

        var nearbyCells = GridHelpers.GetCellListAroundTargetCell30Rings(posX, posY);
        if (!TryGetNearbyVacantCell(gridManager, nearbyCells, out var vacantCell))
        {
            Debug.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: " + entity);
            return;
        }

        SetupPathfinding(gridManager, entityCommandBuffer, localTransform, entity, vacantCell);
        DisableHarvestingUnit(entityCommandBuffer, entity);

        // TODO: Check if this is actually necessary.. This can be removed, right?
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
    private void DisableHarvestingUnit(EntityCommandBuffer entityCommandBuffer, Entity entity)
    {
        entityCommandBuffer.RemoveComponent<ChopAnimation>(entity);
        SystemAPI.SetComponent(entity, new SpriteTransform
        {
            Position = float3.zero,
            Rotation = quaternion.identity
        });
        EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
        //EntityManager.SetComponentData(entity, new HarvestingUnit
        //{
        //    Target = new int2(-1, -1)
        //});

        EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);
    }

    private static bool TryGetNearbyVacantCell(GridManager gridManager, int2[] movePositionList, out int2 nearbyCell)
    {
        for (var i = 1; i < movePositionList.Length; i++)
        {
            nearbyCell = movePositionList[i];
            if (GridHelpers.IsPositionInsideGrid(gridManager, nearbyCell) && GridHelpers.GetIsWalkable(gridManager, nearbyCell)
                                                                          && !GridHelpers.IsPositionOccupied(nearbyCell)
               )
            {
                return true;
            }
        }

        nearbyCell = default;
        return false;
    }

    private void SetupPathfinding(GridManager gridManager, EntityCommandBuffer entityCommandBuffer, RefRO<LocalTransform> localTransform,
        Entity entity, int2 newEndPosition)
    {
        GridHelpers.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
        GridHelpers.ValidateGridPosition(gridManager, ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });
    }
}