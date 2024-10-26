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
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var chopAnimationManager = SystemAPI.GetComponent<ChopAnimationManager>(_chopAnimationManagerSystemHandle);
        var dotsSoundManager = SystemAPI.GetComponent<DotsSoundManager>(_dotsSoundManagerSystemHandle);
        var chopDuration = chopAnimationManager.ChopDuration;

        foreach (var (localTransform, harvestingUnit, pathFollow, spriteTransform, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<HarvestingUnit>, RefRO<PathFollow>, RefRW<SpriteTransform>>()
                     .WithPresent<HarvestingUnitTag>().WithNone<DeliveringUnitTag>().WithEntityAccess())
        {
            var unitIsTryingToHarvest = pathFollow.ValueRO.PathIndex < 0;
            if (!unitIsTryingToHarvest)
            {
                continue;
            }

            // TODO: Try replace with "WithNone". Or: Removing this check might be cleaner.
            if (state.EntityManager.HasComponent<PathfindingParams>(entity))
            {
                continue;
            }

            if (!GridHelpers.CellsAreTouching(localTransform.ValueRO.Position, harvestingUnit.ValueRO.Target))
            {
                GridHelpers.GetXY(localTransform.ValueRO.Position, out var x, out var y);
                ecb.RemoveComponent<HarvestingUnitTag>(entity);
                // Debug.LogError("Unit is trying to chop a tree that is too far away! Position: " + x + " " + y + " Target: " +
                //                harvestingUnit.ValueRO.Target.x + " " + harvestingUnit.ValueRO.Target.y);
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;


            var tileHasNoTree = gridManager.IsWalkable(targetX, targetY);
            if (tileHasNoTree)
            {
                //Debug.Log("Tree was probably destroyed, so please stop chopping it!");
                SeekClosestTree(ref state, ecb, gridManager, entity, harvestingUnit);
                continue;
            }

            var cellIndex = gridManager.GetIndex(targetX, targetY);
            ChopTreeProcess(ref state, ecb, dotsSoundManager, gridManager, chopAnimationManager, entity, harvestingUnit, localTransform, chopDuration,
                cellIndex);

            if (!gridManager.IsDamageable(cellIndex))
            {
                DestroyTree(ref state, ecb, dotsSoundManager, gridManager, entity, localTransform, targetX, targetY, cellIndex);
                SeekClosestDropPoint(ref state, ecb, gridManager, entity, localTransform);
            }
        }

        ecb.Playback(state.EntityManager);
    }

    private void ChopTreeProcess(ref SystemState state, EntityCommandBuffer ecb, DotsSoundManager soundManager, GridManager gridManager,
        ChopAnimationManager chopAnimationManager, Entity entity, RefRW<HarvestingUnit> harvestingUnit, RefRO<LocalTransform> localTransform,
        float chopDuration, int cellIndex)
    {
        harvestingUnit.ValueRW.TimeUntilNextChop -= SystemAPI.Time.DeltaTime;
        if (harvestingUnit.ValueRO.TimeUntilNextChop > 0)
        {
            return;
        }

        harvestingUnit.ValueRW.TimeUntilNextChop = chopDuration;

        gridManager.AddDamage(cellIndex, chopAnimationManager.DamagePerChop);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

        soundManager.ChopSoundRequests.Enqueue(localTransform.ValueRO.Position);
        ecb.AddComponent<ChopAnimationTag>(entity);
    }

    private void DestroyTree(ref SystemState state, EntityCommandBuffer ecb, DotsSoundManager soundManager,
        GridManager gridManager, Entity entity, RefRO<LocalTransform> localTransform, int targetX, int targetY, int cellIndex)
    {
        soundManager.DestroyTreeSoundRequests.Enqueue(localTransform.ValueRO.Position);
        gridManager.SetIsWalkable(targetX, targetY, true);
        gridManager.SetHealth(cellIndex, 0);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

        ecb.RemoveComponent<ChopAnimationTag>(entity);
        SystemAPI.SetComponent(entity, new SpriteTransform
        {
            Position = float3.zero,
            Rotation = quaternion.identity
        });
    }

    private void SeekClosestTree(ref SystemState state, EntityCommandBuffer ecb, GridManager gridManager, Entity entity,
        RefRW<HarvestingUnit> harvestingUnit)
    {
        var currentTarget = harvestingUnit.ValueRO.Target;
        if (gridManager.TryGetNearbyChoppingCell(currentTarget, out var newTarget, out var newPathTarget))
        {
            SystemAPI.SetComponent(entity, new HarvestingUnit
            {
                Target = newTarget
            });

            ecb.AddComponent(entity, new TryDeoccupy
            {
                NewTarget = newPathTarget
            });
        }
        else
        {
            ecb.RemoveComponent<ChopAnimationTag>(entity);
            SystemAPI.SetComponent(entity, new SpriteTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity
            });
            ecb.RemoveComponent<HarvestingUnitTag>(entity);
        }
    }

    private void SeekClosestDropPoint(ref SystemState state, EntityCommandBuffer ecb, GridManager gridManager, Entity entity,
        RefRO<LocalTransform> localTransform)
    {
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

            ecb.AddComponent<DeliveringUnitTag>(entity);
            ecb.AddComponent(entity, new TryDeoccupy
            {
                NewTarget = closestDropPointEntrance
            });
        }
    }
}