using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(PathFollowSystem))]
[UpdateAfter(typeof(GridManagerSystem))]
[BurstCompile]
public partial struct OccupationSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);

        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        // TODO: Test if "WithAll" is necessary
        // TODO: Test if "WithAll<TryDeoccupy>" is faster that RefRO

        foreach (var (tryOccupy, localTransform, entity) in SystemAPI.Query<RefRO<TryOccupy>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            HandleCellOccupation(ref state, entityCommandBuffer, gridManager, localTransform, entity);
            entityCommandBuffer.RemoveComponent<TryOccupy>(entity);
        }

        foreach (var (tryDeoccupy, localTransform, entity) in SystemAPI
                     .Query<RefRO<TryDeoccupy>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            var newTarget = tryDeoccupy.ValueRO.NewTarget;
            HandleCellDeoccupation(ref state, entityCommandBuffer, gridManager, localTransform, entity, newTarget);
            entityCommandBuffer.RemoveComponent<TryDeoccupy>(entity);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }

    private void HandleCellDeoccupation(ref SystemState state, EntityCommandBuffer entityCommandBuffer, GridManager gridManager,
        RefRO<LocalTransform> localTransform,
        Entity entity, int2 newTarget)
    {
        if (gridManager.TryClearOccupant(localTransform.ValueRO.Position, entity))
        {
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        SetupPathfinding(ref state, gridManager, entityCommandBuffer, localTransform, entity, newTarget);
    }

    private void HandleCellOccupation(ref SystemState state, EntityCommandBuffer entityCommandBuffer, GridManager gridManager,
        RefRO<LocalTransform> localTransform,
        Entity entity)
    {
        GridHelpers.GetXY(localTransform.ValueRO.Position, out var posX, out var posY);

        // TODO: Check if it's safe to assume the occupied cell owner is not this entity
        if (!gridManager.IsOccupied(posX, posY))
        {
            // BurstDebugHelpers.DebugLog("Set occupied: " + entity);
            gridManager.SetOccupant(posX, posY, entity);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
            return;
        }

        if (SystemAPI.HasComponent<HarvestingUnitTag>(entity))
        {
            // BurstDebugHelpers.DebugLog("Unit cannot harvest, because cell is occupied: " + posX + " " + posY);

            var harvestTarget = SystemAPI.GetComponent<HarvestingUnit>(entity).Target;
            if (gridManager.TryGetNearbyChoppingCell(harvestTarget, out var newTarget, out var newPathTarget))
            {
                SystemAPI.SetComponent(entity, new HarvestingUnit { Target = newTarget });
                SetupPathfinding(ref state, gridManager, entityCommandBuffer, localTransform, entity, newPathTarget);
                return;
            }

            BurstDebugHelpers.DebugLogWarning("Could not find nearby chopping cell. Disabling harvesting-behaviour...");
        }

        var nearbyCells = gridManager.GetCachedCellListAroundTargetCell(posX, posY);
        if (!TryGetNearbyVacantCell(gridManager, nearbyCells, out var vacantCell))
        {
            BurstDebugHelpers.DebugLogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: ", entity);
            return;
        }

        SetupPathfinding(ref state, gridManager, entityCommandBuffer, localTransform, entity, vacantCell);
        DisableHarvestingUnit(ref state, entityCommandBuffer, entity);

        // TODO: Check if this is actually necessary.. This can be removed, right?
        if (gridManager.TryClearOccupant(localTransform.ValueRO.Position, entity))
        {
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }
    }

    // TODO: Fix race-condition with DeliveringUnitSystem
    private void DisableHarvestingUnit(ref SystemState state, EntityCommandBuffer entityCommandBuffer, Entity entity)
    {
        entityCommandBuffer.RemoveComponent<ChopAnimation>(entity);
        SystemAPI.SetComponent(entity, new SpriteTransform
        {
            Position = float3.zero,
            Rotation = quaternion.identity
        });
        entityCommandBuffer.RemoveComponent<HarvestingUnitTag>(entity);
        SystemAPI.SetComponentEnabled<DeliveringUnit>(entity, false);
    }

    private static bool TryGetNearbyVacantCell(GridManager gridManager, NativeArray<int2> movePositionList, out int2 nearbyCell)
    {
        for (var i = 1; i < movePositionList.Length; i++)
        {
            nearbyCell = movePositionList[i];
            if (gridManager.IsPositionInsideGrid(nearbyCell) && gridManager.IsWalkable(nearbyCell) &&
                !gridManager.IsOccupied(nearbyCell))
            {
                return true;
            }
        }

        nearbyCell = default;
        return false;
    }

    private void SetupPathfinding(ref SystemState state, GridManager gridManager, EntityCommandBuffer entityCommandBuffer,
        RefRO<LocalTransform> localTransform,
        Entity entity, int2 newEndPosition)
    {
        GridHelpers.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
        gridManager.ValidateGridPosition(ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });
    }
}