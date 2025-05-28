using Grid.GridVisuals;
using GridEntityNS;
using Inventory;
using Rendering;
using UnitBehaviours.Pathing;
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
            var dropPointList = new NativeList<int2>(Allocator.Temp);

            for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
            {
                if (gridManager.IsBed(i))
                {
                    bedList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetDropPointEntity(i, out _))
                {
                    dropPointList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetTreeEntity(i, out _))
                {
                    treeList.Add(gridManager.GetXY(i));
                }
            }

            var gridSize = new int2(gridManager.Width, gridManager.Height);

            var trees = new int2[treeList.Length];
            NativeArray<int2>.Copy(treeList.AsArray(), trees);

            var beds = new int2[bedList.Length];
            NativeArray<int2>.Copy(bedList.AsArray(), beds);

            var dropPoints = new int2[dropPointList.Length];
            NativeArray<int2>.Copy(dropPointList.AsArray(), dropPoints);

            SavedGridStateManager.Instance.SaveDataToSaveSlot(gridSize, trees, beds, dropPoints);
            treeList.Dispose();
            bedList.Dispose();
            dropPointList.Dispose();
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
            var dropPoints = SavedGridStateManager.Instance.LoadSavedDropPoints();
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
            }

            for (var i = 0; i < dropPoints.Length; i++)
            {
                gridManager.SetIsWalkable(dropPoints[i], false);
                gridManager.SetDefaultStorageCapacity(dropPoints[i]);
                SpawnManagerSystem.SpawnGridEntity(EntityManager, ecb, gridManager, worldSpriteSheetManager,
                    dropPoints[i], spawnManager.DropPointPrefab,
                    GridEntityType.DropPoint, WorldSpriteSheetEntryType.Storage);
            }

            gridManager.WalkableGridIsDirty = true;
            gridManager.OccupiableGridIsDirty = true;
            gridManager.DamageableGridIsDirty = true;
            gridManager.InteractableGridIsDirty = true;

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