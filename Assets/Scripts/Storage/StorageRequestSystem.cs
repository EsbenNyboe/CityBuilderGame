using Grid;
using Inventory;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Storage
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct StorageRequestSystem : ISystem
    {
        private EntityQuery _storageRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();

            _storageRequestQuery = state.GetEntityQuery(
                new EntityQueryDesc { All = new ComponentType[] { typeof(StorageRequest) } }
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var storageRequest in SystemAPI.Query<RefRO<StorageRequest>>())
            {
                var requestedAddAmount = storageRequest.ValueRO.RequestAmount;

                var gridCell = storageRequest.ValueRO.GridCell;
                var itemCapacity = gridManager.GetStorageItemCapacity(gridCell);
                var itemCount = gridManager.GetStorageItemCount(gridCell);

                var requestedItemCountTotal = itemCount + requestedAddAmount;
                var requestIsValid =
                    requestedItemCountTotal > 0 && requestedItemCountTotal <= itemCapacity;
                var requesterEntity = storageRequest.ValueRO.RequesterEntity;
                var inventory = SystemAPI.GetComponentRW<InventoryState>(requesterEntity);

                if (!requestIsValid)
                {
                    if (
                        SystemAPI.Exists(requesterEntity)
                        && SystemAPI.HasComponent<IsSeekingDropPoint>(requesterEntity)
                    )
                    {
                        // Stop seeking
                        ecb.RemoveComponent<IsSeekingDropPoint>(requesterEntity);
                        ecb.AddComponent<IsDeciding>(requesterEntity);
                    }
                }
                else
                {
                    Debug.Log(
                        "StorageSystem: ItemCount before is "
                        + itemCount
                        + " ItemCount after is "
                        + requestedItemCountTotal
                    );
                    // Target: storage cell
                    gridManager.SetStorageItemCount(gridCell, requestedItemCountTotal);

                    // Source: inventory
                    inventory.ValueRW.CurrentItem =
                        requestedAddAmount > 0 ? InventoryItem.None : InventoryItem.LogOfWood;
                }
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(_storageRequestQuery);
            SystemAPI.SetSingleton(gridManager);
        }
    }
}