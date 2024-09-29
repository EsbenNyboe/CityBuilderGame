using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class HarvestingUnitSystem : SystemBase
{
    private const int DamagePerSec = -20;

    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (localTransform, harvestingUnit, pathFollow, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<HarvestingUnit>, RefRO<PathFollow>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            var unitIsTryingToHarvest = pathFollow.ValueRO.PathIndex < 0;
            if (!unitIsTryingToHarvest)
            {
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;

            var tileHasNoTree = GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).IsWalkable();
            if (tileHasNoTree)
            {
                // Tree was probably destroyed, so please stop chopping it!
                Debug.Log("Tree was probably destroyed, so please stop chopping it!");

                // Seek new tree:
                var currentTarget = harvestingUnit.ValueRO.Target;
                if (TryGetNearbyChoppingCell(currentTarget, out var newTarget, out var newPathTarget))
                {
                    EntityManager.SetComponentData(entity, new HarvestingUnit
                    {
                        Target = newTarget
                    });
                    SetupPathfinding(entityCommandBuffer, localTransform.ValueRO.Position, entity, newPathTarget);
                }
                else
                {
                    EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                    harvestingUnit.ValueRW.Target = new int2(-1, -1);
                }

                continue;
            }

            var gridDamageableObject = GridSetup.Instance.DamageableGrid.GetGridObject(targetX, targetY);
            gridDamageableObject.AddToHealth(DamagePerSec * SystemAPI.Time.DeltaTime);
            if (!gridDamageableObject.IsDamageable())
            {
                // DESTROY TREE:
                GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).SetIsWalkable(true);
                gridDamageableObject.SetHealth(0);
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void SetupPathfinding(EntityCommandBuffer entityCommandBuffer, float3 position, Entity entity, int2 newEndPosition)
    {
        GridSetup.Instance.PathGrid.GetXY(position, out var startX, out var startY);
        PathingHelpers.ValidateGridPosition(ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });
    }

    private bool TryGetNearbyChoppingCell(int2 currentTarget, out int2 newTarget, out int2 newPathTarget)
    {
        var (nearbyCellsX, nearbyCellsY) = PathingHelpers.GetCellListAroundTargetCell(currentTarget.x, currentTarget.y, 20);

        if (GridSetup.Instance.DamageableGrid.GetGridObject(currentTarget.x, currentTarget.y).IsDamageable() &&
            TryGetValidNeighbourCell(currentTarget.x, currentTarget.y, out var newPathTargetX, out var newPathTargetY))
        {
            newTarget = currentTarget;
            newPathTarget = new int2(newPathTargetX, newPathTargetY);
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
                newTarget = new int2(x, y);
                newPathTarget = new int2(newPathTargetX, newPathTargetY);
                return true;
            }
        }

        newTarget = default;
        newPathTarget = default;
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
}