using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class HarvestingUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (harvestingUnit, pathFollow, entity) in SystemAPI.Query<RefRW<HarvestingUnit>, RefRO<PathFollow>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex >= 0)
            {
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;

            if (GridSetup.Instance.PathfindingGrid.GetGridObject(targetX, targetY).IsWalkable())
            {
                // Tree probably was destroyed, during pathfinding
                EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                harvestingUnit.ValueRW.IsHarvesting = false;
                harvestingUnit.ValueRW.Target = new int2(-1, -1);
            }

            if (harvestingUnit.ValueRO.IsHarvesting)
            {
                continue;
            }

            harvestingUnit.ValueRW.IsHarvesting = true;

            // TODO: Replace this with trees as grid
            SetDegradationState(targetX, targetY, true);
        }
    }

    private void SetDegradationState(int targetX, int targetY, bool state)
    {
        foreach (var (localTransform, unitDegradation) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitDegradation>>())
        {
            GridSetup.Instance.PathfindingGrid.GetXY(localTransform.ValueRO.Position, out var x, out var y);
            if (targetX != x || targetY != y)
            {
                continue;
            }

            unitDegradation.ValueRW.IsDegrading = state;
        }
    }

    private bool TryGetNearbyTree(int unitPosX, int unitPosY, out int treePosX, out int treePosY)
    {
        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, 0, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 0, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, 0, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, -1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 0, -1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, -1, out treePosX, out treePosY))
        {
            return true;
        }

        return false;
    }

    private bool NeighbourIsWalkable(int unitPosX, int unitPosY, int xAddition, int yAddition, out int treePosX, out int treePosY)
    {
        treePosX = unitPosX + xAddition;
        treePosY = unitPosY + yAddition;
        return GridSetup.Instance.PathfindingGrid.GetGridObject(treePosX, treePosY).IsWalkable();
    }
}