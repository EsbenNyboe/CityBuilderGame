using Grid;
using Inventory;
using UnitBehaviours.AutonomousHarvesting.Model;
using Unity.Collections;
using Unity.Entities;

namespace DroppedItemNS
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct DroppedItemRequestSystem : ISystem
    {
        private EntityQuery _droppedItemRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();

            _droppedItemRequestQuery = state.GetEntityQuery(
                new EntityQueryDesc { All = new ComponentType[] { typeof(DroppedItemRequest) } }
            );
            state.RequireForUpdate(_droppedItemRequestQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var pickedUpItemEntities = new NativeList<Entity>(Allocator.Temp);

            foreach (var droppedItemRequest in SystemAPI.Query<RefRO<DroppedItemRequest>>())
            {
                var requesterEntity = droppedItemRequest.ValueRO.RequesterEntity;
                var droppedItemEntity = droppedItemRequest.ValueRO.DroppedItemEntity;

                if (!SystemAPI.Exists(requesterEntity)
                    || !SystemAPI.Exists(droppedItemEntity)
                    || pickedUpItemEntities.Contains(droppedItemEntity))
                {
                    continue;
                }

                var droppedItem = SystemAPI.GetComponent<DroppedItem>(droppedItemEntity);
                var inventory = SystemAPI.GetComponentRW<InventoryState>(requesterEntity);
                inventory.ValueRW.CurrentItem = droppedItem.ItemType;
                pickedUpItemEntities.Add(droppedItemEntity);
                ecb.DestroyEntity(droppedItemEntity);
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(_droppedItemRequestQuery);
            SystemAPI.SetSingleton(gridManager);
        }
    }
}