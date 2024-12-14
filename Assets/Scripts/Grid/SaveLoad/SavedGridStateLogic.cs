using Rendering;
using UnitState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
                if (gridManager.IsDamageable(i))
                {
                    treeList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.IsBed(i))
                {
                    bedList.Add(gridManager.GetXY(i));
                }
                else if (gridManager.TryGetDropPointEntity(i, out _))
                {
                    dropPointList.Add(gridManager.GetXY(i));
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

            // DELETE CURRENT STATE
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
            {
                gridManager.SetIsWalkable(i, true);

                // Delete tree
                gridManager.SetHealthToZero(i);

                // Delete bed
                gridManager.SetInteractableNone(i);

                // Delete droppoint
                if (gridManager.TryGetDropPointEntity(i, out var dropPointEntity))
                {
                    gridManager.RemoveGridEntity(i);
                    ecb.DestroyEntity(dropPointEntity);
                }
            }

            gridManager.WalkableGridIsDirty = true;
            gridManager.OccupiableGridIsDirty = true;
            gridManager.DamageableGridIsDirty = true;
            gridManager.InteractableGridIsDirty = true;

            // LOAD NEW STATE

            var trees = SavedGridStateManager.Instance.LoadSavedTrees();
            var beds = SavedGridStateManager.Instance.LoadSavedBeds();
            var dropPoints = SavedGridStateManager.Instance.LoadSavedDropPoints();
            var gridSize = SavedGridStateManager.Instance.TryLoadSavedGridSize(new int2(gridManager.Width, gridManager.Height));
            GridDimensionsConfig.Instance.Width = gridSize.x;
            GridDimensionsConfig.Instance.Height = gridSize.y;
            GridManagerSystem.TryUpdateGridDimensions(ref gridManager);

            HandleEntitiesOutsideOfGrid(gridManager);

            SavedGridStateManager.Instance.OnLoaded();
            // TODO: Convert spawnManager to singleton
            var spawnManager = GetSpawnManager();

            for (var i = 0; i < trees.Length; i++)
            {
                gridManager.SetIsWalkable(trees[i], false);
                gridManager.SetHealthToMax(trees[i]);
            }

            for (var i = 0; i < beds.Length; i++)
            {
                gridManager.SetIsWalkable(beds[i], true);
                gridManager.SetInteractableBed(beds[i]);
            }

            for (var i = 0; i < dropPoints.Length; i++)
            {
                if (!gridManager.IsPositionInsideGrid(dropPoints[i]))
                {
                    Debug.Log("Not inside grid bounds");
                }
                else
                {
                    // TODO: Refactor this, to reduce code-duplication (see SpawnManagerSystem)
                    gridManager.SetIsWalkable(dropPoints[i], false);
                    var entity = InstantiateAtPosition(spawnManager.DropPointPrefab, dropPoints[i]);
                    ecb.RemoveComponent<LinkedEntityGroup>(entity);
                    gridManager.AddGridEntity(dropPoints[i], entity, GridEntityType.DropPoint);
                    SpawnManagerSystem.SetupWorldSpriteSheetState(ecb, worldSpriteSheetManager, entity, dropPoints[i],
                        WorldSpriteSheetEntryType.DropPoint);
                }
            }

            SystemAPI.SetSingleton(gridManager);
            ecb.Playback(EntityManager);
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
}