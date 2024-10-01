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

            if (EntityManager.HasComponent<PathfindingParams>(entity))
            {
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;

            var tileHasNoTree = GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).IsWalkable();
            if (tileHasNoTree)
            {
                // Tree was probably destroyed, so please stop chopping it!
                //Debug.Log("Tree was probably destroyed, so please stop chopping it!");

                // Seek new tree:
                var currentTarget = harvestingUnit.ValueRO.Target;
                if (PathingHelpers.TryGetNearbyChoppingCell(currentTarget, out var newTarget, out var newPathTarget))
                {
                    var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
                    if (occupationCell.EntityIsOwner(entity))
                    {
                        occupationCell.SetOccupied(Entity.Null);
                    }

                    // TODO: Investigate if this is what produces the error with long-range chopping. Is it maybe a bad idea to depend on PathFollow alone?
                    EntityManager.SetComponentData(entity, new HarvestingUnit
                    {
                        Target = newTarget
                    });
                    SetupPathfinding(entityCommandBuffer, localTransform.ValueRO.Position, entity, newPathTarget);
                }
                else
                {
                    EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                    //harvestingUnit.ValueRW.Target = new int2(-1, -1);
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

                int2 deliveryPoint = new int2(5, 5);
                SetupPathfinding(entityCommandBuffer, localTransform.ValueRO.Position, entity, deliveryPoint);
                EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                EntityManager.SetComponentEnabled<DeliveringUnit>(entity, true);
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
}