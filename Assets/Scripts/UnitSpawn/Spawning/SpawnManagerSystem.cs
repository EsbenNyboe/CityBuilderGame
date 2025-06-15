using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using Grid;
using Inventory;
using Rendering;
using SystemGroups;
using UnitBehaviours.UnitConfigurators;
using UnitState.AliveState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitSpawn.Spawning
{
    [UpdateInGroup(typeof(LifetimeSystemGroup), OrderFirst = true)]
    public partial class SpawnManagerSystem : SystemBase
    {
        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnManager>();
            var loadUnitManager = new LoadUnitManager
            {
                VillagersToLoad = new NativeList<float3>(Allocator.Persistent),
                BoarsToLoad = new NativeList<float3>(Allocator.Persistent)
            };
            EntityManager.CreateSingleton(loadUnitManager);
        }

        protected override void OnDestroy()
        {
            var loadUnitManager = SystemAPI.GetSingleton<LoadUnitManager>();
            loadUnitManager.VillagersToLoad.Dispose();
            loadUnitManager.BoarsToLoad.Dispose();
        }

        protected override void OnUpdate()
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var loadUnitManager = SystemAPI.GetSingleton<LoadUnitManager>();
            var itemToSpawn = SpawnMenuManager.Instance.ItemToSpawn;
            var itemToDelete = SpawnMenuManager.Instance.ItemToDelete;
            var brushSize = SpawnMenuManager.Instance.GetBrushSize();
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            var cellSize = 1f;
            var mousePosition =
                UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;
            var cellPosition = GridHelpers.GetXY(mousePosition);

            if (!gridManager.IsPositionInsideGrid(cellPosition) && !loadUnitManager.HasUnits())
            {
                return;
            }

            var cellList = GridHelpersManaged.GetCellListAroundTargetCell(cellPosition, brushSize);
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            // TODO: Make SpawnManager into a Singleton instead
            foreach (var spawnManager in SystemAPI.Query<RefRO<SpawnManager>>())
            {
                SpawnProcess(ecb, ref gridManager, worldSpriteSheetManager, cellList, spawnManager.ValueRO, itemToSpawn, ref loadUnitManager);
                DeleteProcess(ecb, ref gridManager, cellList, brushSize, itemToDelete);
            }

            SystemAPI.SetSingleton(gridManager);
            SystemAPI.SetSingleton(loadUnitManager);
        }

        private void SpawnProcess(EntityCommandBuffer ecb, ref GridManager gridManager,
            WorldSpriteSheetManager worldSpriteSheetManager,
            List<int2> cellList,
            SpawnManager spawnManager,
            SpawnItemType itemToSpawn, ref LoadUnitManager loadUnitManager)
        {
            var isLoading = loadUnitManager.HasUnits();
            var loadedVillagers = loadUnitManager.VillagersToLoad;
            var loadedBoars = loadUnitManager.BoarsToLoad;
            if (!loadedVillagers.IsEmpty)
            {
                foreach (var villagerPos in loadedVillagers)
                {
                    TryLoadUnit(ecb, ref gridManager, villagerPos, spawnManager.VillagerPrefab, false);
                }
            }

            if (!loadedBoars.IsEmpty)
            {
                foreach (var boarPos in loadedBoars)
                {
                    TryLoadUnit(ecb, ref gridManager, boarPos, spawnManager.BoarPrefab, false);
                }
            }

            loadedVillagers.Clear();
            loadedBoars.Clear();

            loadUnitManager.VillagersToLoad = loadedVillagers;
            loadUnitManager.BoarsToLoad = loadedBoars;

            if (isLoading)
            {
                return;
            }

            switch (itemToSpawn)
            {
                case SpawnItemType.None:
                    break;
                case SpawnItemType.Unit:
                    foreach (var cell in cellList)
                    {
                        TrySpawnVillager(ecb, ref gridManager, cell, spawnManager.VillagerPrefab, false);
                    }

                    break;
                case SpawnItemType.Baby:
                    foreach (var cell in cellList)
                    {
                        TrySpawnVillager(ecb, ref gridManager, cell, spawnManager.VillagerPrefab, false, true);
                    }

                    break;
                case SpawnItemType.Boar:
                    foreach (var cell in cellList)
                    {
                        TrySpawnVillager(ecb, ref gridManager, cell, spawnManager.BoarPrefab, false);
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
                        TrySpawnBed(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.BedPrefab);
                    }

                    break;
                case SpawnItemType.Storage:
                    foreach (var cell in cellList)
                    {
                        TrySpawnStorage(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.StoragePrefab);
                    }

                    break;
                case SpawnItemType.House:
                    foreach (var cell in cellList)
                    {
                        TrySpawnHouse(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.HousePrefab);
                    }

                    break;
                case SpawnItemType.Bonfire:
                    foreach (var cell in cellList)
                    {
                        TrySpawnBonfire(ecb, ref gridManager, worldSpriteSheetManager, cell, spawnManager.BonfirePrefab);
                    }

                    break;
                case SpawnItemType.DroppedLog:
                    foreach (var cell in cellList)
                    {
                        TrySpawnDroppedItem(ecb, ref gridManager, cell, InventoryItem.LogOfWood);
                    }

                    break;
                case SpawnItemType.DroppedRawMeat:
                    foreach (var cell in cellList)
                    {
                        TrySpawnDroppedItem(ecb, ref gridManager, cell, InventoryItem.RawMeat);
                    }

                    break;
                case SpawnItemType.DroppedCookedMeat:
                    foreach (var cell in cellList)
                    {
                        TrySpawnDroppedItem(ecb, ref gridManager, cell, InventoryItem.CookedMeat);
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
                    TryDeleteVillagers(ecb, cellList[0], brushSize);
                    break;
                case SpawnItemType.Baby:
                    TryDeleteVillagers(ecb, cellList[0], brushSize);
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
                        TryDeleteBed(ecb, ref gridManager, cell);
                    }

                    break;
                case SpawnItemType.Storage:
                    foreach (var cell in cellList)
                    {
                        TryDeleteStorage(ecb, ref gridManager, cell);
                    }

                    break;
                case SpawnItemType.House:
                    foreach (var cell in cellList)
                    {
                        TryDeleteHouse(ecb, ref gridManager, cell);
                    }

                    break;
                case SpawnItemType.Bonfire:
                    foreach (var cell in cellList)
                    {
                        TryDeleteBonfire(ecb, ref gridManager, cell);
                    }

                    break;
                case SpawnItemType.DroppedLog:
                    foreach (var cell in cellList)
                    {
                        TryDeleteDroppedItem(ecb, ref gridManager, cell, brushSize);
                    }

                    break;
                case SpawnItemType.DroppedRawMeat:
                    foreach (var cell in cellList)
                    {
                        TryDeleteDroppedItem(ecb, ref gridManager, cell, brushSize);
                    }

                    break;
                case SpawnItemType.DroppedCookedMeat:
                    foreach (var cell in cellList)
                    {
                        TryDeleteDroppedItem(ecb, ref gridManager, cell, brushSize);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TryLoadUnit(EntityCommandBuffer ecb, ref GridManager gridManager, float3 pos, Entity prefab,
            bool hasHierarchy)
        {
            if (gridManager.IsPositionInsideGrid(pos))
            {
                var entity = InstantiateAtPosition(prefab, pos);

                if (!hasHierarchy)
                {
                    // If the unit doesn't have a hierarchy, it doesn't need a LinkedEntityGroup
                    ecb.RemoveComponent<LinkedEntityGroup>(entity);
                }
            }
        }

        private void TrySpawnVillager(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell, Entity prefab,
            bool hasHierarchy, bool isBaby = false)
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

                if (isBaby)
                {
                    ecb.AddComponent<Baby>(entity);
                }
            }
        }

        private void TryDeleteVillagers(EntityCommandBuffer ecb, int2 center, int brushSize)
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

        private void TrySpawnStorage(EntityCommandBuffer ecb, ref GridManager gridManager, WorldSpriteSheetManager worldSpriteSheetManager,
            int2 cell,
            Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsBed(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetIsWalkable(cell, false);
                gridManager.SetDefaultStorageCapacity(cell);
                gridManager.SetStorageCount(cell, 0, InventoryItem.LogOfWood);
                gridManager.SetStorageCount(cell, 0, InventoryItem.RawMeat);
                gridManager.SetStorageCount(cell, 0, InventoryItem.CookedMeat);

                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.Storage,
                    WorldSpriteSheetEntryType.Storage);
            }
        }


        private void TryDeleteStorage(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell)
        {
            if (gridManager.IsPositionInsideGrid(cell) &&
                gridManager.TryGetStorageEntity(cell, out var entity))
            {
                gridManager.SetIsWalkable(cell, true);
                gridManager.RemoveGridEntity(cell);
                gridManager.SetStorageCapacity(cell, 0);
                gridManager.SetStorageCount(cell, 0, InventoryItem.LogOfWood);
                gridManager.SetStorageCount(cell, 0, InventoryItem.RawMeat);
                gridManager.SetStorageCount(cell, 0, InventoryItem.CookedMeat);

                ecb.DestroyEntity(entity);
            }
        }

        private void TrySpawnHouse(EntityCommandBuffer ecb, ref GridManager gridManager, WorldSpriteSheetManager worldSpriteSheetManager,
            int2 cell,
            Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsBed(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetIsWalkable(cell, false);

                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.House,
                    WorldSpriteSheetEntryType.House);
            }
        }

        private void TryDeleteHouse(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell)
        {
            if (gridManager.IsPositionInsideGrid(cell) &&
                gridManager.TryGetHouseEntity(cell, out var entity))
            {
                gridManager.SetIsWalkable(cell, true);
                gridManager.RemoveGridEntity(cell);

                ecb.DestroyEntity(entity);
            }
        }

        private void TrySpawnBonfire(EntityCommandBuffer ecb, ref GridManager gridManager, WorldSpriteSheetManager worldSpriteSheetManager,
            int2 cell,
            Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsBed(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetIsWalkable(cell, false);

                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.Bonfire,
                    WorldSpriteSheetEntryType.BonfireReady);
            }
        }

        private void TryDeleteBonfire(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell)
        {
            if (gridManager.IsPositionInsideGrid(cell) &&
                gridManager.TryGetBonfireEntity(cell, out var entity))
            {
                gridManager.SetIsWalkable(cell, true);
                gridManager.RemoveGridEntity(cell);

                ecb.DestroyEntity(entity);
            }
        }

        private void TrySpawnBed(EntityCommandBuffer ecb, ref GridManager gridManager,
            WorldSpriteSheetManager worldSpriteSheetManager, int2 cell, Entity prefab)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsInteractable(cell) && !gridManager.HasGridEntity(cell))
            {
                gridManager.SetInteractableBed(cell);

                SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager, cell, prefab, GridEntityType.Bed,
                    WorldSpriteSheetEntryType.Bed);
            }
        }

        private void TryDeleteBed( EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell)
        {
            if (gridManager.IsPositionInsideGrid(cell) &&
                gridManager.IsBed(cell) &&
                gridManager.TryGetBedEntity(cell, out var entity))
            {
                gridManager.SetIsWalkable(cell, true);
                gridManager.RemoveGridEntity(cell);
                gridManager.SetInteractableNone(cell);

                ecb.DestroyEntity(entity);
            }
        }

        private void TrySpawnDroppedItem(EntityCommandBuffer ecb, ref GridManager gridManager, int2 cell, InventoryItem itemType)
        {
            if (gridManager.IsPositionInsideGrid(cell) && gridManager.IsWalkable(cell) &&
                !gridManager.IsInteractable(cell) && !gridManager.HasGridEntity(cell))
            {
                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new DroppedItem
                {
                    ItemType = itemType
                });
                ecb.AddComponent(entity, new LocalTransform
                {
                    Position = GetEntityPosition(cell),
                    Scale = 1,
                    Rotation = quaternion.identity
                });
            }
        }

        private void TryDeleteDroppedItem(EntityCommandBuffer ecb, ref GridManager gridManager, int2 center, int brushSize)
        {
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess()
                         .WithAll<Villager>())
            {
                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (math.distance(center, cell) <= brushSize)
                {
                    ecb.DestroyEntity(entity);
                }
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

        private Entity InstantiateAtPosition(Entity prefab, float3 position)
        {
            var entity = EntityManager.Instantiate(prefab);
            SystemAPI.SetComponent(
                entity,
                new LocalTransform
                {
                    Position = position,
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