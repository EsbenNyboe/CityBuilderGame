using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(OccupationSystem))]
[UpdateAfter(typeof(GridManagerSystem))]
[UpdateAfter(typeof(ChopAnimationManagerSystem))]
// TODO: Consider running before sound-manager actually
[UpdateAfter(typeof(DotsSoundManagerSystem))]
[BurstCompile]
public partial struct HarvestingUnitSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;
    private SystemHandle _chopAnimationManagerSystemHandle;
    private SystemHandle _dotsSoundManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        _chopAnimationManagerSystemHandle = state.World.GetExistingSystem<ChopAnimationManagerSystem>();
        _dotsSoundManagerSystemHandle = state.World.GetExistingSystem<DotsSoundManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var chopAnimationManager = SystemAPI.GetComponent<ChopAnimationManager>(_chopAnimationManagerSystemHandle);
        var dotsSoundManager = SystemAPI.GetComponent<DotsSoundManager>(_dotsSoundManagerSystemHandle);
        var chopDuration = chopAnimationManager.ChopDuration;
        var chopSize = chopAnimationManager.ChopAnimationSize;
        var chopIdleTime = chopAnimationManager.ChopAnimationIdleTime;

        foreach (var (localTransform, harvestingUnit, pathFollow, spriteTransform, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<HarvestingUnit>, RefRO<PathFollow>, RefRW<SpriteTransform>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            // TODO: Replace with create/destroy component
            var unitIsTryingToHarvest = pathFollow.ValueRO.PathIndex < 0;
            if (!unitIsTryingToHarvest)
            {
                continue;
            }

            // TODO: Try replace with "WithNone"
            if (state.EntityManager.HasComponent<PathfindingParams>(entity))
            {
                continue;
            }

            if (!GridHelpers.CellsAreTouching(localTransform.ValueRO.Position, harvestingUnit.ValueRO.Target))
            {
                GridHelpers.GetXY(localTransform.ValueRO.Position, out var x, out var y);
                SystemAPI.SetComponentEnabled<HarvestingUnit>(entity, false);
                // Debug.LogError("Unit is trying to chop a tree that is too far away! Position: " + x + " " + y + " Target: " +
                //                harvestingUnit.ValueRO.Target.x + " " + harvestingUnit.ValueRO.Target.y);
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;

            var tileHasNoTree = gridManager.WalkableGrid[gridManager.GetIndex(targetX, targetY)].IsWalkable;
            if (tileHasNoTree)
            {
                // Tree was probably destroyed, so please stop chopping it!
                //Debug.Log("Tree was probably destroyed, so please stop chopping it!");

                // Seek new tree:
                var currentTarget = harvestingUnit.ValueRO.Target;
                if (gridManager.TryGetNearbyChoppingCell(currentTarget, out var newTarget, out var newPathTarget))
                {
                    // TODO: Replace with TryDeoccupy-component
                    if (gridManager.TryClearOccupant(localTransform.ValueRO.Position, entity))
                    {
                        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
                    }

                    // TODO: Investigate if this is what produces the error with long-range chopping. Is it maybe a bad idea to depend on PathFollow alone?
                    SystemAPI.SetComponent(entity, new HarvestingUnit
                    {
                        Target = newTarget
                    });
                    SetupPathfinding(gridManager, entityCommandBuffer, localTransform.ValueRO.Position, entity, newPathTarget);
                }
                else
                {
                    entityCommandBuffer.RemoveComponent<ChopAnimation>(entity);
                    SystemAPI.SetComponent(entity, new SpriteTransform
                    {
                        Position = float3.zero,
                        Rotation = quaternion.identity
                    });
                    SystemAPI.SetComponentEnabled<HarvestingUnit>(entity, false);
                    //harvestingUnit.ValueRW.Target = new int2(-1, -1);
                }

                continue;
            }

            var cellIndex = gridManager.GetIndex(targetX, targetY);
            harvestingUnit.ValueRW.TimeUntilNextChop -= SystemAPI.Time.DeltaTime;
            if (harvestingUnit.ValueRO.TimeUntilNextChop < 0)
            {
                harvestingUnit.ValueRW.TimeUntilNextChop = chopDuration;

                gridManager.AddDamage(cellIndex, chopAnimationManager.DamagePerChop);
                SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

                dotsSoundManager.ChopSoundRequests.Enqueue(localTransform.ValueRO.Position);
                var chopTarget = harvestingUnit.ValueRO.Target;
                entityCommandBuffer.AddComponent(entity, new ChopAnimation
                {
                    TargetPosition = GridHelpers.GetWorldPosition(chopTarget.x, chopTarget.y),
                    ChopAnimationProgress = chopDuration,
                    ChopDuration = chopDuration,
                    ChopSize = chopSize,
                    ChopIdleTime = chopIdleTime
                });
            }

            if (gridManager.DamageableGrid[cellIndex].Health <= 0)
            {
                // DESTROY TREE:
                dotsSoundManager.DestroyTreeSoundRequests.Enqueue(localTransform.ValueRO.Position);
                gridManager.SetIsWalkable(targetX, targetY, true);
                gridManager.SetHealth(cellIndex, 0);
                SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

                entityCommandBuffer.RemoveComponent<ChopAnimation>(entity);
                SystemAPI.SetComponent(entity, new SpriteTransform
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity
                });
                SystemAPI.SetComponentEnabled<HarvestingUnit>(entity, false);

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
                    GridHelpers.GetXY(closestDropPoint, out var x, out var y);
                    var dropPointCell = new int2(x, y);
                    SystemAPI.SetComponentEnabled<DeliveringUnit>(entity, true);
                    SystemAPI.SetComponent(entity, new DeliveringUnit
                    {
                        Target = dropPointCell
                    });

                    var closestDropPointEntrance = new int2(-1, -1);
                    var shortestDropPointEntranceDistance = math.INFINITY;
                    GridHelpers.GetXY(position, out var posX, out var posY);
                    var cellPosition = new int2(posX, posY);
                    gridManager.RandomizeNeighbourSequenceIndex();
                    for (var i = 0; i < 8; i++)
                    {
                        gridManager.GetSequencedNeighbourCell(dropPointCell.x, dropPointCell.y, out var dropPointEntranceX,
                            out var dropPointEntranceY);
                        var dropPointEntrance = new int2(dropPointEntranceX, dropPointEntranceY);
                        var dropPointEntranceDistance = math.distance(cellPosition, dropPointEntrance);
                        if (dropPointEntranceDistance < shortestDropPointEntranceDistance)
                        {
                            closestDropPointEntrance = dropPointEntrance;
                            shortestDropPointEntranceDistance = dropPointEntranceDistance;
                        }
                    }

                    SetupPathfinding(gridManager, entityCommandBuffer, localTransform.ValueRO.Position, entity, closestDropPointEntrance);

                    // TODO: Should this be added to OccupationSystemSystem? If so, it would cause DeliveringUnit to break.
                    gridManager.TryClearOccupant(localTransform.ValueRO.Position, entity);
                    SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
                }
            }
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }

    private void SetupPathfinding(GridManager gridManager, EntityCommandBuffer entityCommandBuffer, float3 position, Entity entity,
        int2 newEndPosition)
    {
        GridHelpers.GetXY(position, out var startX, out var startY);
        gridManager.ValidateGridPosition(ref startX, ref startY);

        entityCommandBuffer.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(startX, startY),
            EndPosition = newEndPosition
        });
    }
}

public struct ChopAnimation : IComponentData
{
    public float3 TargetPosition;
    public float ChopAnimationProgress;

    public float ChopDuration;
    public float ChopSize;
    public float ChopIdleTime;
}