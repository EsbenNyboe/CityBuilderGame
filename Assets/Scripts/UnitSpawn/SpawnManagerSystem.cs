using System;
using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class SpawnManagerSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        RequireForUpdate<SpawnManager>();
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var itemToSpawn = SpawnMenuManager.Instance.ItemToSpawn;

        var cellSize = 1f;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;
        var cellPosition = GridHelpers.GetXY(mousePosition);

        if (!gridManager.IsPositionInsideGrid(cellPosition))
        {
            return;
        }

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
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }


    private void TrySpawnTree(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsDamageable(position))
        {
            gridManager.SetIsWalkable(position, false);
            gridManager.SetHealthToMax(position);
        }
    }

    private void TrySpawnBed(ref GridManager gridManager, int2 position)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsInteractable(position))
        {
            gridManager.SetInteractableBed(position);
        }
    }


    private void TrySpawnUnit(ref GridManager gridManager, int2 position, Entity prefab)
    {
        if (gridManager.IsWalkable(position) && !gridManager.IsOccupied(position))
        {
            var unitEntity = InstantiateAtPosition(prefab, position);

            gridManager.SetOccupant(position, unitEntity);
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

    private Entity InstantiateAtPosition(Entity prefab, int2 position)
    {
        var entity = EntityManager.Instantiate(prefab);
        EntityManager.SetComponentData(entity, new LocalTransform
        {
            Position = new float3(position.x, position.y, -0.01f),
            Scale = 1,
            Rotation = quaternion.identity
        });
        return entity;
    }
}