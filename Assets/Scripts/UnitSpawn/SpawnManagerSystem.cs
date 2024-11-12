using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SpawningSystemGroup))]
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
        var brushSize = SpawnMenuManager.Instance.GetBrushSize();

        var cellSize = 1f;
        var mousePosition =
            UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;
        var cellPosition = GridHelpers.GetXY(mousePosition);

        if (!gridManager.IsPositionInsideGrid(cellPosition))
        {
            return;
        }

        var cellList = GridHelpersManaged.GetCellListAroundTargetCell(cellPosition, brushSize);
        var ecb = new EntityCommandBuffer(WorldUpdateAllocator);
        // TODO: Make SpawnManager into a Singleton instead
        foreach (var spawnManager in SystemAPI.Query<RefRO<SpawnManager>>())
        {
            SpawnProcess(ref gridManager, cellList, spawnManager.ValueRO, itemToSpawn);
            DeleteProcess(ecb, ref gridManager, cellList, itemToDelete);
        }

        ecb.Playback(EntityManager);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }

    private void SpawnProcess(ref GridManager gridManager, List<int2> cellList, SpawnManager spawnManager,
        SpawnItemType itemToSpawn)
    {
        switch (itemToSpawn)
        {
            case SpawnItemType.None:
                break;
            case SpawnItemType.Unit:
                foreach (var cell in cellList)
                {
                    TrySpawnUnit(ref gridManager, cell, spawnManager.UnitPrefab, true);
                }

                break;
            case SpawnItemType.Zombie:
                foreach (var cell in cellList)
                {
                    TrySpawnUnit(ref gridManager, cell, spawnManager.ZombiePrefab, false);
                }

                break;
            case SpawnItemType.Tree:
                foreach (var cell in cellList)
                {
                    TrySpawnTree(ref gridManager, cell);
                }

                break;
            case SpawnItemType.Bed:
                foreach (var cell in cellList)
                {
                    TrySpawnBed(ref gridManager, cell);
                }

                break;
            case SpawnItemType.House:
                foreach (var cell in cellList)
                {
                    TrySpawnDropPoint(ref gridManager, cell, spawnManager.DropPointPrefab);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DeleteProcess(EntityCommandBuffer ecb, ref GridManager gridManager, List<int2> cellList,
        SpawnItemType itemToDelete)
    {
        switch (itemToDelete)
        {
            case SpawnItemType.None:
                break;
            case SpawnItemType.Unit:
                foreach (var cell in cellList)
                {
                    TryDeleteUnit(ecb, ref gridManager, cell);
                }

                break;
            case SpawnItemType.Tree:
                foreach (var cell in cellList)
                {
                    TryDeleteTree(ref gridManager, cell);
                }

                break;
            case SpawnItemType.Bed:
                foreach (var cell in cellList)
                {
                    TryDeleteBed(ref gridManager, cell);
                }

                break;
            case SpawnItemType.House:
                foreach (var cell in cellList)
                {
                    TryDeleteDropPoint(ecb, ref gridManager, cell);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    private void TrySpawnUnit(ref GridManager gridManager, int2 position, Entity prefab, bool isPerson)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsWalkable(position) &&
            !gridManager.IsOccupied(position))
        {
            var unitEntity = InstantiateAtPosition(prefab, position);
            if (isPerson)
            {
                gridManager.SetOccupant(position, unitEntity);
            }
        }
    }

    private void TryDeleteUnit(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell)
    {
        if (gridManager.IsPositionInsideGrid(cell) && gridManager.TryGetOccupant(cell, out var unitEntity))
        {
            gridManager.DestroyUnit(ecb, unitEntity, cell);
        }
    }

    private void TrySpawnTree(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsWalkable(position) &&
            !gridManager.IsDamageable(position))
        {
            gridManager.SetIsWalkable(position, false);
            gridManager.SetHealthToMax(position);
        }
    }

    private void TryDeleteTree(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsPositionInsideGrid(position) && !gridManager.IsWalkable(position) &&
            gridManager.IsDamageable(position))
        {
            gridManager.SetIsWalkable(position, true);
            gridManager.SetHealthToZero(position);
        }
    }

    private void TrySpawnBed(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsWalkable(position) &&
            !gridManager.IsInteractable(position))
        {
            gridManager.SetInteractableBed(position);
        }
    }

    private void TryDeleteBed(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsBed(position))
        {
            gridManager.SetIsWalkable(position, true);
            gridManager.SetInteractableNone(position);
        }
    }

    private void TrySpawnDropPoint(ref GridManager gridManager, int2 position, Entity prefab)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsWalkable(position))
        {
            gridManager.SetIsWalkable(position, false);
            InstantiateAtPosition(prefab, position);
        }
    }

    private void TryDeleteDropPoint(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position)
    {
        // TODO: FIIIIIIX
        // If it's not a tree or a bed, it must be a DropPoint, I guess?
        if (!gridManager.IsWalkable(position) && !gridManager.IsInteractable(position) &&
            !gridManager.IsDamageable(position))
        {
            var gridIndex = gridManager.GetIndex(position);
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<DropPoint>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
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