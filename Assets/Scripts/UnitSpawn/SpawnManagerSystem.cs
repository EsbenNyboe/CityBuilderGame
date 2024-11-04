using System;
using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class SpawnManagerSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;
    private bool _initialized;

    protected override void OnCreate()
    {
        RequireForUpdate<SpawnManager>();
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var itemToSpawn = SpawnMenuManager.Instance.ItemToSpawn;
        var itemToDelete = SpawnMenuManager.Instance.ItemToDelete;

        var cellSize = 1f;
        var mousePosition =
            UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;
        var cellPosition = GridHelpers.GetXY(mousePosition);

        if (!gridManager.IsPositionInsideGrid(cellPosition))
        {
            return;
        }

        var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
        // TODO: Make SpawnManager into a Singleton instead
        foreach (var spawnManager in SystemAPI.Query<RefRO<SpawnManager>>())
        {
            switch (itemToSpawn)
            {
                case SpawnItemType.None:
                    break;
                case SpawnItemType.Unit:
                    TrySpawnUnit(ref gridManager, cellPosition, spawnManager.ValueRO.UnitPrefab);
                    break;
                case SpawnItemType.Tree:
                    TrySpawnTree(ref gridManager, cellPosition);
                    break;
                case SpawnItemType.Bed:
                    TrySpawnBed(ref gridManager, cellPosition);
                    break;
                case SpawnItemType.House:
                    TrySpawnDropPoint(ref gridManager, cellPosition, spawnManager.ValueRO.DropPointPrefab);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (itemToDelete)
            {
                case SpawnItemType.None:
                    break;
                case SpawnItemType.Unit:
                    TryDeleteUnit(ecb, ref gridManager, cellPosition);
                    break;
                case SpawnItemType.Tree:
                    TryDeleteTree(ref gridManager, cellPosition);
                    break;
                case SpawnItemType.Bed:
                    TryDeleteBed(ref gridManager, cellPosition);
                    break;
                case SpawnItemType.House:
                    TryDeleteDropPoint(ecb, ref gridManager, cellPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        ecb.Playback(EntityManager);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }


    private void TrySpawnUnit(ref GridManager gridManager, int2 position, Entity prefab)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsOccupied(position))
        {
            var unitEntity = InstantiateAtPosition(prefab, position);
            gridManager.SetOccupant(position, unitEntity);
        }
    }

    private void TryDeleteUnit(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position)
    {
        if (gridManager.TryGetOccupant(position, out var unitEntity))
        {
            ecb.DestroyEntity(unitEntity);
            gridManager.SetOccupant(position, Entity.Null);
        }
    }

    private void TrySpawnTree(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsDamageable(position))
        {
            gridManager.SetIsWalkable(position, false);
            gridManager.SetHealthToMax(position);
        }
    }

    private void TryDeleteTree(ref GridManager gridManager, int2 position)
    {
        if (!gridManager.IsWalkable(position) && gridManager.IsDamageable(position))
        {
            gridManager.SetIsWalkable(position, true);
            gridManager.SetHealthToZero(position);
        }
    }

    private void TrySpawnBed(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsInteractable(position))
        {
            gridManager.SetInteractableBed(position);
        }
    }

    private void TryDeleteBed(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsBed(position))
        {
            gridManager.SetIsWalkable(position, true);
            gridManager.SetInteractableNone(position);
        }
    }

    private void TrySpawnDropPoint(ref GridManager gridManager, int2 position, Entity prefab)
    {
        if (gridManager.IsWalkable(position))
        {
            gridManager.SetIsWalkable(position, false);
            InstantiateAtPosition(prefab, position);
        }
    }

    private void TryDeleteDropPoint(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position)
    {
        // TODO: FIIIIIIX
        // If it's not a tree or a bed, it must be a DropPoint, I guess?
        if (!gridManager.IsWalkable(position) && !gridManager.IsInteractable(position) && !gridManager.IsDamageable(position))
        {
            var gridIndex = gridManager.GetIndex(position);
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<DropPoint>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (gridManager.GetIndex(localTransform.ValueRO.Position) == gridIndex)
                {
                    ecb.DestroyEntity(entity);
                    gridManager.SetIsWalkable(gridIndex, true);
                }
            }
        }
    }

    private Entity InstantiateAtPosition(Entity prefab, int2 position)
    {
        var entity = EntityManager.Instantiate(prefab);
        SystemAPI.SetComponent(
            entity,
            new LocalTransform
            {
                Position = new float3(position.x, position.y, -0.01f),
                Scale = 1,
                Rotation = quaternion.identity
            }
        );
        return entity;
    }
}