using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LifetimeSystemGroup))]
public partial class SpawnManagerSystem : SystemBase
{
    private bool _initialized;

    protected override void OnCreate()
    {
        RequireForUpdate<SpawnManager>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetSingleton<GridManager>();
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
        var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);
        // TODO: Make SpawnManager into a Singleton instead
        foreach (var spawnManager in SystemAPI.Query<RefRO<SpawnManager>>())
        {
            SpawnProcess(ecb, ref gridManager, cellList, spawnManager.ValueRO, itemToSpawn);
            DeleteProcess(ecb, ref gridManager, cellList, brushSize, itemToDelete);
        }

        SystemAPI.SetSingleton(gridManager);
    }

    private void SpawnProcess(EntityCommandBuffer ecb, ref GridManager gridManager, List<int2> cellList,
        SpawnManager spawnManager,
        SpawnItemType itemToSpawn)
    {
        switch (itemToSpawn)
        {
            case SpawnItemType.None:
                break;
            case SpawnItemType.Unit:
                foreach (var cell in cellList)
                {
                    TrySpawnUnit(ecb, ref gridManager, cell, spawnManager.UnitPrefab, false);
                }

                break;
            case SpawnItemType.Boar:
                foreach (var cell in cellList)
                {
                    TrySpawnUnit(ecb, ref gridManager, cell, spawnManager.BoarPrefab, false);
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
        int brushSize, SpawnItemType itemToDelete)
    {
        switch (itemToDelete)
        {
            case SpawnItemType.None:
                break;
            case SpawnItemType.Unit:
                TryDeleteUnits(ecb, cellList[0], brushSize);
                break;
            case SpawnItemType.Boar:
                TryDeleteUnits(ecb, cellList[0], brushSize);
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

    private void TrySpawnUnit(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position, Entity prefab,
        bool hasHierarchy)
    {
        if (gridManager.IsPositionInsideGrid(position) && gridManager.IsWalkable(position) &&
            !gridManager.IsOccupied(position))
        {
            var entity = InstantiateAtPosition(prefab, position);
            gridManager.SetOccupant(position, entity);

            if (!hasHierarchy)
            {
                // If the unit doesn't have a hierarchy, it doesn't need a LinkedEntityGroup
                ecb.RemoveComponent<LinkedEntityGroup>(entity);
            }
        }
    }

    private void TryDeleteUnits(EntityCommandBuffer ecb, int2 center, int brushSize)
    {
        foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess()
                     .WithAll<SocialRelationships>())
        {
            var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
            if (math.distance(center, cell) <= brushSize)
            {
                ecb.SetComponentEnabled<IsAlive>(entity, false);
            }
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