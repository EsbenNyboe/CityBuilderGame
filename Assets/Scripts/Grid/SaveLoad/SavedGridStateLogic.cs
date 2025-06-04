using Grid.GridVisuals;
using GridEntityNS;
using Inventory;
using Rendering;
using UnitBehaviours.Pathing;
using UnitBehaviours.UnitConfigurators;
using UnitSpawn.Spawning;
using UnitState.AliveLogic;
using UnitState.AliveState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Grid.SaveLoad
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(IsAliveSystem))]
    public partial class SavedGridStateLogic : SystemBase
    {
        protected override void OnUpdate()
        {
            if (SavedGridStateManager.Instance.SlotToSave > -1)
            {
                SaveGridState();
            }

            if (SavedGridStateManager.Instance.SlotToLoad > -1)
            {
                LoadSavedGridState();
            }

            if (SavedGridStateManager.Instance.SlotToDelete > -1)
            {
                SavedGridStateManager.Instance.DeleteDataOnSaveSlot();
            }

            SavedGridStateManager.Instance.SlotToSave = -1;
            SavedGridStateManager.Instance.SlotToLoad = -1;
            SavedGridStateManager.Instance.SlotToDelete = -1;
        }

        private void SaveGridState()
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var treeList = new NativeList<int2>(Allocator.Temp);
            var bedList = new NativeList<int2>(Allocator.Temp);
            var storageList = new NativeList<int2>(Allocator.Temp);
            var bonfireList = new NativeList<int2>(Allocator.Temp);
            var houseList = new NativeList<int2>(Allocator.Temp);
            var villagerList = new NativeList<float3>(Allocator.Temp);
            var boarList = new NativeList<float3>(Allocator.Temp);

            for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
            {
                if (gridManager.IsBed(i))
                {
                    bedList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetStorageEntity(i, out _))
                {
                    storageList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetTreeEntity(i, out _))
                {
                    treeList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetBonfireEntity(i, out _))
                {
                    bonfireList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetHouseEntity(i, out _))
                {
                    houseList.Add(gridManager.GetXY(i));
                }
            }

            var gridSize = new int2(gridManager.Width, gridManager.Height);

            var trees = new int2[treeList.Length];
            NativeArray<int2>.Copy(treeList.AsArray(), trees);

            var beds = new int2[bedList.Length];
            NativeArray<int2>.Copy(bedList.AsArray(), beds);

            var storages = new int2[storageList.Length];
            NativeArray<int2>.Copy(storageList.AsArray(), storages);
            var bonfires = new int2[bonfireList.Length];
            NativeArray<int2>.Copy(bonfireList.AsArray(), bonfires);
            var houses = new int2[houseList.Length];
            NativeArray<int2>.Copy(houseList.AsArray(), houses);


            foreach (var (villager, localTransform) in SystemAPI.Query<RefRO<Villager>, RefRO<LocalTransform>>())
            {
                villagerList.Add(localTransform.ValueRO.Position);
            }

            foreach (var (boar, localTransform) in SystemAPI.Query<RefRO<Boar>, RefRO<LocalTransform>>())
            {
                boarList.Add(localTransform.ValueRO.Position);
            }

            var villagers = new float3[villagerList.Length];
            NativeArray<float3>.Copy(villagerList.AsArray(), villagers);
            var boars = new float3[boarList.Length];
            NativeArray<float3>.Copy(boarList.AsArray(), boars);

            SavedGridStateManager.Instance.SaveDataToSaveSlot(gridSize, trees, beds, storages, bonfires,  houses, villagers, boars);
            treeList.Dispose();
            bedList.Dispose();
            storageList.Dispose();
            bonfireList.Dispose();
            houseList.Dispose();
        }

        private void LoadSavedGridState()
        {
            Dependency.Complete();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            // DELETE ALL GRID STATE
            foreach (var (_, entity) in SystemAPI.Query<RefRO<GridEntity>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            GridManagerSystem.DisposeGridData(gridManager);

            // LOAD NEW STATE
            var trees = SavedGridStateManager.Instance.LoadSavedTrees();
            var beds = SavedGridStateManager.Instance.LoadSavedBeds();
            var storages = SavedGridStateManager.Instance.LoadSavedStorages();
            var bonfires = SavedGridStateManager.Instance.LoadSavedBonfires();
            var houses = SavedGridStateManager.Instance.LoadSavedHouses();
            var villagers = SavedGridStateManager.Instance.LoadSavedVillagers();
            var boars = SavedGridStateManager.Instance.LoadSavedBoars();
            var gridSize = SavedGridStateManager.Instance.TryLoadSavedGridSize(new int2(gridManager.Width, gridManager.Height));
            GridDimensionsConfig.Instance.Width = gridSize.x;
            GridDimensionsConfig.Instance.Height = gridSize.y;
            GridManagerSystem.CreateGrids(ref gridManager, GridDimensionsConfig.Instance.Width, GridDimensionsConfig.Instance.Height);

            HandleEntitiesOutsideOfGrid(gridManager);

            SavedGridStateManager.Instance.OnLoaded();
            // TODO: Convert spawnManager to singleton
            var spawnManager = GetSpawnManager();

            for (var i = 0; i < trees.Length; i++)
            {
                gridManager.SetIsWalkable(trees[i], false);
                gridManager.SetHealthToMax(trees[i]);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    trees[i], spawnManager.TreePrefab,
                    GridEntityType.Tree, WorldSpriteSheetEntryType.Tree);
            }

            for (var i = 0; i < beds.Length; i++)
            {
                gridManager.SetIsWalkable(beds[i], true);
                gridManager.SetInteractableBed(beds[i]);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    beds[i], spawnManager.BedPrefab,
                    GridEntityType.Bed, WorldSpriteSheetEntryType.Bed);
            }

            for (var i = 0; i < storages.Length; i++)
            {
                gridManager.SetIsWalkable(storages[i], false);
                gridManager.SetDefaultStorageCapacity(storages[i]);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    storages[i], spawnManager.StoragePrefab,
                    GridEntityType.Storage, WorldSpriteSheetEntryType.Storage);
            }

            for (var i = 0; i < bonfires.Length; i++)
            {
                gridManager.SetIsWalkable(bonfires[i], false);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    bonfires[i], spawnManager.BonfirePrefab,
                    GridEntityType.Bonfire, WorldSpriteSheetEntryType.BonfireReady);
            }

            for (var i = 0; i < houses.Length; i++)
            {
                gridManager.SetIsWalkable(houses[i], false);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    houses[i], spawnManager.HousePrefab,
                    GridEntityType.House, WorldSpriteSheetEntryType.House);
            }

            gridManager.WalkableGridIsDirty = true;
            gridManager.OccupiableGridIsDirty = true;
            gridManager.DamageableGridIsDirty = true;
            gridManager.InteractableGridIsDirty = true;

            foreach (var (isAlive, isAliveEntity) in SystemAPI.Query<RefRO<IsAlive>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<IsAlive>(isAliveEntity, false);
            }

            foreach (var (droppedItem, droppedItemEntity) in SystemAPI.Query<RefRO<DroppedItem>>().WithEntityAccess())
            {
                ecb.DestroyEntity(droppedItemEntity);
            }

            var loadUnitManager = SystemAPI.GetSingleton<LoadUnitManager>();

            for (var i = 0; i < villagers.Length; i++)
            {
                loadUnitManager.VillagersToLoad.Add(villagers[i]);
            }

            for (var i = 0; i < boars.Length; i++)
            {
                loadUnitManager.BoarsToLoad.Add(boars[i]);
            }

            SystemAPI.SetSingleton(gridManager);
            ecb.Playback(EntityManager);
            GridVisualsManager.Instance.OnGridSizeChanged();
        }

        private void HandleEntitiesOutsideOfGrid(GridManager gridManager)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            // Cancel pathing, if targeting cell outside of grid
            foreach (var (pathFollow, pathPositions) in SystemAPI.Query<RefRW<PathFollow>, DynamicBuffer<PathPosition>>())
            {
                if (pathFollow.ValueRO.IsMoving() &&
                    !gridManager.IsPositionInsideGrid(pathPositions[0].Position))
                {
                    pathFollow.ValueRW.PathIndex = -1;
                }
            }

            // Destroy, if positioned outside of grid
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<IsAlive>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!gridManager.IsPositionInsideGrid(localTransform.ValueRO.Position))
                {
                    ecb.SetComponentEnabled<IsAlive>(entity, false);
                }
            }

            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<DroppedItem>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!gridManager.IsPositionInsideGrid(localTransform.ValueRO.Position))
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(EntityManager);
        }

        private SpawnManager GetSpawnManager()
        {
            foreach (var spawnManager in SystemAPI.Query<RefRO<SpawnManager>>())
            {
                return spawnManager.ValueRO;
            }

            return new SpawnManager();
        }
    }
}