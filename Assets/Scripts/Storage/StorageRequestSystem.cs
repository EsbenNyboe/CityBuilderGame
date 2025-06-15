using System;
using Grid;
using Inventory;
using Rendering;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StorageNS
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct StorageRequestSystem : ISystem
    {
        private EntityQuery _storageRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StorageRuleManager>();
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
            var storageLookup = SystemAPI.GetBufferLookup<Storage>();

            foreach (var storageRequest in SystemAPI.Query<RefRO<StorageRequest>>())
            {
                var requestType = storageRequest.ValueRO.RequestType;
                var requesterEntity = storageRequest.ValueRO.RequesterEntity;
                var requestedItem = storageRequest.ValueRO.ItemType;

                if (!SystemAPI.Exists(requesterEntity))
                {
                    continue;
                }

                var inventory = SystemAPI.GetComponentRW<InventoryState>(requesterEntity);

                var gridCell = storageRequest.ValueRO.GridCell;
                var requestIsValid = false;

                if (gridManager.TryGetStorageEntity(gridCell, out var storageEntity))
                {
                    var storage = storageLookup[storageEntity];
                    var storageIndex = -1;
                    if (requestType == StorageRequestType.Withdraw)
                    {
                        for (var i = storage.Length - 1; i >= 0; i--)
                        {
                            if (storage[i].Item == requestedItem)
                            {
                                storageIndex = i;
                                break;
                            }
                        }
                    }
                    else if (requestType == StorageRequestType.Deposit)
                    {
                        for (var i = 0; i < storage.Length; i++)
                        {
                            if (storage[i].Item == InventoryItem.None)
                            {
                                storageIndex = i;
                                break;
                            }
                        }
                    }

                    requestIsValid = storageIndex != -1;

                    if (requestIsValid)
                    {
                        // Target: storage cell
                        storage[storageIndex] = new Storage
                        {
                            Item = requestType switch
                            {
                                StorageRequestType.Deposit => requestedItem,
                                StorageRequestType.Withdraw => InventoryItem.None,
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        };

                        // Source: inventory
                        inventory.ValueRW.CurrentItem = requestType switch
                        {
                            StorageRequestType.Deposit => InventoryItem.None,
                            StorageRequestType.Withdraw => requestedItem,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                }

                if (!requestIsValid)
                {
                    Debug.Log("StorageSystem: Invalid Request");
                    if (
                        SystemAPI.Exists(requesterEntity)
                        && SystemAPI.HasComponent<IsSeekingRoomyStorage>(requesterEntity)
                    )
                    {
                        // Stop seeking
                        ecb.RemoveComponent<IsSeekingRoomyStorage>(requesterEntity);
                        ecb.AddComponent<IsDeciding>(requesterEntity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(_storageRequestQuery);
            SystemAPI.SetSingleton(gridManager);
        }
    }
}