using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class HarvestingUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        var chopDuration = ChopAnimationManager.ChopDuration();

        // TODO: Optimize this:
        foreach (var (localTransform, harvestingUnit, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<HarvestingUnit>>()
                     .WithDisabled<HarvestingUnit>().WithEntityAccess())
        {
            harvestingUnit.ValueRW.TimeUntilNextChop = 0;
            harvestingUnit.ValueRW.ChopAnimationProgress = chopDuration;
            harvestingUnit.ValueRW.DoChopAnimation = false;
        }

        foreach (var (localTransform, harvestingUnit, pathFollow, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<HarvestingUnit>, RefRO<PathFollow>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            if (harvestingUnit.ValueRO.DoChopAnimation)
            {
                DoChopAnimation(localTransform, harvestingUnit, entity);
            }

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
            harvestingUnit.ValueRW.TimeUntilNextChop -= SystemAPI.Time.DeltaTime;
            if (harvestingUnit.ValueRO.TimeUntilNextChop < 0)
            {
                gridDamageableObject.RemoveFromHealth(ChopAnimationManager.DamagePerChop());
                harvestingUnit.ValueRW.DoChopAnimation = true;
                harvestingUnit.ValueRW.TimeUntilNextChop = chopDuration;
                harvestingUnit.ValueRW.ChopAnimationProgress = chopDuration;
                SoundManager.Instance.PlayChopSound(localTransform.ValueRO.Position);
            }

            if (!gridDamageableObject.IsDamageable())
            {
                // DESTROY TREE:
                SoundManager.Instance.PlayDestroyTreeSound(localTransform.ValueRO.Position);
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

    private void DoChopAnimation(RefRO<LocalTransform> localTransform, RefRW<HarvestingUnit> harvestingUnit, Entity entity)
    {
        var chopDuration = ChopAnimationManager.ChopDuration();
        var chopSize = ChopAnimationManager.ChopAnimationSize();
        var chopIdleTime = ChopAnimationManager.ChopAnimationPostIdleTimeNormalized();

        // Manage animation state:
        var timeLeft = harvestingUnit.ValueRO.ChopAnimationProgress;
        timeLeft -= SystemAPI.Time.DeltaTime;
        harvestingUnit.ValueRW.ChopAnimationProgress = timeLeft;

        if (timeLeft < 0)
        {
            harvestingUnit.ValueRW.ChopAnimationProgress = chopDuration;
            timeLeft = 0;
            harvestingUnit.ValueRW.DoChopAnimation = false;
        }

        // Calculate animation input:
        var timeLeftNormalized = timeLeft / chopDuration;
        var timeLeftBeforeIdling = timeLeftNormalized - chopIdleTime;
        var timeLeftBeforeIdlingNormalized = math.max(0, timeLeftBeforeIdling) * (1 + chopIdleTime);

        // Calculate animation output:
        var positionDistanceFromOrigin = timeLeftBeforeIdlingNormalized * chopSize;

        var chopTarget = harvestingUnit.ValueRO.Target;
        var chopTargetPosition = GridSetup.Instance.PathGrid.GetWorldPosition(chopTarget.x, chopTarget.y);
        var chopDirection = (chopTargetPosition - (Vector3)localTransform.ValueRO.Position).normalized;

        var childPosition = positionDistanceFromOrigin * chopDirection;

        // Apply animation output:
        var childEntity = EntityManager.GetBuffer<Child>(entity)[0].Value;

        var angleInDegrees = chopDirection.x > 0 ? 0f : 180f;
        EntityManager.SetComponentData(childEntity, new LocalTransform
        {
            Position = childPosition,
            Scale = 1f,
            Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0)
        });
    }
}