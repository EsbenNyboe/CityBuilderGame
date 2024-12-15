using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using Rendering;
using UnitBehaviours;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitSpawn
{
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
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

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
                SpawnProcess(ecb, ref gridManager, worldSpriteSheetManager, cellList, spawnManager.ValueRO, itemToSpawn);
                DeleteProcess(ecb, ref gridManager, cellList, brushSize, itemToDelete);
            }

            SystemAPI.SetSingleton(gridManager);
        }

        private void SpawnProcess(EntityCommandBuffer ecb, ref GridManager gridManager,
            WorldSpriteSheetManager worldSpriteSheetManager,
            List<int2> cellList,
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
                        TrySpawnTree(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.TreePrefab);
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
                        TrySpawnDropPoint(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.DropPointPrefab);
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
                    TryDeleteBoars(ecb, cellList[0], brushSize);
                    break;
                case SpawnItemType.Tree:
                    foreach (var cell in cellList)
                    {
                        TryDeleteTree(ecb, ref gridManager, cell);
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

        private void TrySpawnUnit(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell, Entity prefab,
            bool hasHierarchy)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsOccupied(cell))
            {
                var entity = InstantiateAtPosition(prefab, cell);
                gridManager.SetOccupant(cell, entity);

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
                         .WithAll<Villager>())
            {
                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (math.distance(center, cell) <= brushSize)
                {
                    ecb.SetComponentEnabled<IsAlive>(entity, false);
                }
            }
        }

        private void TryDeleteBoars(EntityCommandBuffer ecb, int2 center, int brushSize)
        {
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess()
                         .WithAll<Boar>())
            {
                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (math.distance(center, cell) <= brushSize)
                {
                    ecb.SetComponentEnabled<IsAlive>(entity, false);
                }
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

        private void TrySpawnTree(EntityCommandBuffer ecb, ref GridManager gridManager, WorldSpriteSheetManager worldSpriteSheetManager, int2 cell,
            Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsBed(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetIsWalkable(cell, false);
                gridManager.SetHealthToMax(cell);
                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.Tree,
                    WorldSpriteSheetEntryType.Tree);
            }
        }

        private void TryDeleteTree(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position)
        {
            if (gridManager.IsPositionInsideGrid(position) && gridManager.TryGetTreeEntity(position, out var entity))
            {
                gridManager.SetHealthToZero(position);

                gridManager.SetIsWalkable(position, true);
                gridManager.RemoveGridEntity(position);
                ecb.DestroyEntity(entity);
            }
        }

        private void TrySpawnDropPoint(EntityCommandBuffer ecb, ref GridManager gridManager, WorldSpriteSheetManager worldSpriteSheetManager, int2 cell,
            Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsBed(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetIsWalkable(cell, false);
                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.DropPoint,
                    WorldSpriteSheetEntryType.DropPoint);
            }
        }

        private void TryDeleteDropPoint(EntityCommandBuffer ecb, ref GridManager gridManager, int2 position)
        {
            if (gridManager.IsPositionInsideGrid(position) &&
                gridManager.TryGetDropPointEntity(position, out var entity))
            {
                gridManager.SetIsWalkable(position, true);
                gridManager.RemoveGridEntity(position);
                ecb.DestroyEntity(entity);
            }
        }

        public static void SpawnGridEntity(EntityManager entityManager, EntityCommandBuffer ecb, GridManager gridManager,
            WorldSpriteSheetManager worldSpriteSheetManager,
            int2 cell,
            Entity prefab, GridEntityType gridEntityType, WorldSpriteSheetEntryType spriteEntryType)
        {
            var entity = InstantiateAtPosition(entityManager, ecb, prefab, cell);
            ecb.RemoveComponent<LinkedEntityGroup>(entity);
            gridManager.AddGridEntity(cell, entity, gridEntityType);
            var position = GetEntityPosition(cell);
            ecb.AddComponent(entity, new WorldSpriteSheetState
            {
                Uv = worldSpriteSheetManager.GetUv(spriteEntryType),
                Matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one)
            });
        }

        private static Entity InstantiateAtPosition(EntityManager entityManager, EntityCommandBuffer ecb, Entity prefab, int2 cell)
        {
            var entity = entityManager.Instantiate(prefab);
            ecb.SetComponent(entity, new LocalTransform
                {
                    Position = GetEntityPosition(cell),
                    Scale = 1,
                    Rotation = quaternion.identity
                }
            );
            return entity;
        }

        private Entity InstantiateAtPosition(Entity prefab, int2 cell)
        {
            var entity = EntityManager.Instantiate(prefab);
            SystemAPI.SetComponent(
                entity,
                new LocalTransform
                {
                    Position = GetEntityPosition(cell),
                    Scale = 1,
                    Rotation = quaternion.identity
                }
            );
            return entity;
        }

        private static float3 GetEntityPosition(int2 cell)
        {
            return new float3(cell.x, cell.y, -0.01f);
        }
    }
}