using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Grid.SaveLoad
{
    public partial class SavedGridStateLogic : SystemBase
    {
        private EntityQuery _dropPointQuery;

        protected override void OnCreate()
        {
            _dropPointQuery = GetEntityQuery(typeof(DropPoint));
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
            {
                SaveGridState();
            }

            if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftControl))
            {
                LoadSavedGridState();
            }
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
                // TODO: Implement drop-point as grid-cell-type
                else if (false) //gridManager.IsDropPoint(i))
                {
                    dropPointList.Add(gridManager.GetXY(i));
                }
            }

            // TEMPORARY:
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<DropPoint>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                dropPointList.Add(GridHelpers.GetXY(localTransform.ValueRO.Position));
            }

            var trees = new int2[treeList.Length];
            NativeArray<int2>.Copy(treeList.AsArray(), trees);

            var beds = new int2[bedList.Length];
            NativeArray<int2>.Copy(bedList.AsArray(), beds);

            var dropPoints = new int2[dropPointList.Length];
            NativeArray<int2>.Copy(dropPointList.AsArray(), dropPoints);

            SavedGridStateManager.Instance.SaveDataToSaveSlot(trees, beds, dropPoints);
            treeList.Dispose();
            bedList.Dispose();
            dropPointList.Dispose();
        }

        private void LoadSavedGridState()
        {
            // DELETE CURRENT STATE
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
            {
                // Delete tree
                gridManager.SetIsWalkable(i, true);
                gridManager.SetHealthToZero(i);

                // Delete bed
                gridManager.SetInteractableNone(i);

                // TODO: Delete droppoint
            }

            gridManager.WalkableGridIsDirty = true;
            gridManager.OccupiableGridIsDirty = true;
            gridManager.DamageableGridIsDirty = true;
            gridManager.InteractableGridIsDirty = true;
            EntityManager.DestroyEntity(_dropPointQuery);

            // LOAD NEW STATE

            var trees = SavedGridStateManager.Instance.LoadSavedTrees();
            var beds = SavedGridStateManager.Instance.LoadSavedBeds();
            var dropPoints = SavedGridStateManager.Instance.LoadSavedDropPoints();
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
                gridManager.SetIsWalkable(dropPoints[i], false);
                InstantiateAtPosition(spawnManager.DropPointPrefab, dropPoints[i]);
            }

            SystemAPI.SetSingleton(gridManager);
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