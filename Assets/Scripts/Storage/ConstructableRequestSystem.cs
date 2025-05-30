using Grid;
using GridEntityNS;
using Inventory;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Storage
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct ConstructableRequestSystem : ISystem
    {
        private EntityQuery _constructableRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();

            _constructableRequestQuery = state.GetEntityQuery(
                new EntityQueryDesc { All = new ComponentType[] { typeof(ConstructableRequest) } }
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var constructableRequest in SystemAPI.Query<RefRO<ConstructableRequest>>())
            {
                var requestedAmount = constructableRequest.ValueRO.RequestAmount;

                var gridCell = constructableRequest.ValueRO.GridCell;
                if (!gridManager.TryGetGridEntityAndType(gridCell, out var gridEntity, out var gridEntityType) ||
                    !SystemAPI.HasComponent<Constructable>(gridEntity))
                {
                    // TODO: Send response in case the entity got destroyed
                    Debug.LogError("INVALID REQUEST!!!");
                }

                var constructable = SystemAPI.GetComponentRW<Constructable>(gridEntity);
                var itemCount = constructable.ValueRO.Materials;
                var itemCapacity = constructable.ValueRO.MaterialsRequired;

                var requestedItemCountTotal = itemCount - requestedAmount;
                var requestIsValid =
                    requestedItemCountTotal > 0 && requestedItemCountTotal <= itemCapacity;
                var requesterEntity = constructableRequest.ValueRO.RequesterEntity;
                var inventory = SystemAPI.GetComponentRW<InventoryState>(requesterEntity);

                if (!requestIsValid)
                {
                    // This should not happen:
                    Debug.Log("ConstructableSystem: Invalid Request");
                    if (
                        SystemAPI.Exists(requesterEntity)
                        && SystemAPI.HasComponent<IsSeekingConstructable>(requesterEntity)
                    )
                    {
                        // Stop seeking
                        ecb.RemoveComponent<IsSeekingConstructable>(requesterEntity);
                        ecb.AddComponent<IsDeciding>(requesterEntity);
                    }
                }
                else
                {
                    // Target: Constructable cell
                    constructable.ValueRW.Materials = requestedItemCountTotal;

                    // Source: inventory
                    inventory.ValueRW.CurrentItem =
                        requestedAmount < 0 ? InventoryItem.None : InventoryItem.LogOfWood;
                }
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(_constructableRequestQuery);
            SystemAPI.SetSingleton(gridManager);
        }
    }
}