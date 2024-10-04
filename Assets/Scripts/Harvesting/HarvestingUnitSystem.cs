﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class HarvestingUnitSystem : SystemBase
{
    private const int DamagePerSec = -1;

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
            gridDamageableObject.AddToHealth(DamagePerSec * SystemAPI.Time.DeltaTime * Globals.HarvestingSpeed() * Globals.GameSpeed());
            if (!gridDamageableObject.IsDamageable())
            {
                // DESTROY TREE:
                GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).SetIsWalkable(true);
                gridDamageableObject.SetHealth(0);

                EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);

                var closestDropPoint = new float3(-1, -1, -1);
                var shortestDropPointDistance = math.INFINITY;

                var position = localTransform.ValueRO.Position;
                foreach (var (dropPointTransform, dropPoint) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DropPoint>>())
                {
                    var dropPointPosition = dropPointTransform.ValueRO.Position;
                    var dropPointDistance = math.distance(position, dropPointPosition);
                    if (dropPointDistance < shortestDropPointDistance)
                    {
                        shortestDropPointDistance = dropPointDistance;
                        closestDropPoint = dropPointPosition;
                    }
                }

                if (closestDropPoint.x > -1)
                {
                    GridSetup.Instance.PathGrid.GetXY(closestDropPoint, out var x, out var y);
                    var dropPointCell = new int2(x, y);
                    EntityManager.SetComponentEnabled<DeliveringUnit>(entity, true);
                    EntityManager.SetComponentData(entity, new DeliveringUnit
                    {
                        Target = dropPointCell
                    });

                    var closestDropPointEntrance = new int2(-1, -1);
                    var shortestDropPointEntranceDistance = math.INFINITY;
                    GridSetup.Instance.PathGrid.GetXY(position, out var posX, out var posY);
                    var cellPosition = new int2(posX, posY);
                    for (var i = 0; i < 8; i++)
                    {
                        PathingHelpers.GetNeighbourCell(i, dropPointCell.x, dropPointCell.y, out var dropPointEntranceX, out var dropPointEntranceY);
                        var dropPointEntrance = new int2(dropPointEntranceX, dropPointEntranceY);
                        var dropPointEntranceDistance = math.distance(cellPosition, dropPointEntrance);
                        if (dropPointEntranceDistance < shortestDropPointEntranceDistance)
                        {
                            closestDropPointEntrance = dropPointEntrance;
                            shortestDropPointEntranceDistance = dropPointEntranceDistance;
                        }
                    }

                    SetupPathfinding(entityCommandBuffer, localTransform.ValueRO.Position, entity, closestDropPointEntrance);

                    var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(localTransform.ValueRO.Position);
                    if (occupationCell.EntityIsOwner(entity))
                    {
                        occupationCell.SetOccupied(Entity.Null);
                    }
                }
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