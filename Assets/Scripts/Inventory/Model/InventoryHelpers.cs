using StorageNS;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Inventory
{
    public class InventoryHelpers
    {
        public static void DropItemOnGround(EntityCommandBuffer ecb,
            ref InventoryState inventory, float3 position)
        {
            var droppedItemEntity = ecb.CreateEntity();
            ecb.AddComponent(droppedItemEntity, new DroppedItem
            {
                ItemType = inventory.CurrentItem
            });
            ecb.AddComponent(droppedItemEntity, new LocalTransform
            {
                Position = position,
                Scale = 1,
                Rotation = quaternion.identity
            });
            inventory.CurrentItem = InventoryItem.None;
        }

        public static void DropItemOnGround(EntityCommandBuffer.ParallelWriter ecbParallelWriter, int i,
            ref InventoryState inventory, float3 position)
        {
            var droppedItemEntity = ecbParallelWriter.CreateEntity(i);
            ecbParallelWriter.AddComponent(i, droppedItemEntity, new DroppedItem
            {
                ItemType = inventory.CurrentItem
            });
            ecbParallelWriter.AddComponent(i, droppedItemEntity, new LocalTransform
            {
                Position = position,
                Scale = 1,
                Rotation = quaternion.identity
            });
            inventory.CurrentItem = InventoryItem.None;
        }

        public static void SendRequestForConstructItem(EntityCommandBuffer ecb, Entity sourceEntity, int2 targetCell)
        {
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new ConstructableRequest
            {
                GridCell = targetCell,
                RequestAmount = -1,
                RequesterEntity = sourceEntity
            });
        }

        public static void SendRequestForStoreItem(EntityCommandBuffer ecb, Entity sourceEntity, int2 targetCell, InventoryItem itemType)
        {
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new StorageRequest
            {
                GridCell = targetCell,
                RequestType = StorageRequestType.Deposit,
                RequesterEntity = sourceEntity,
                ItemType = itemType
            });
        }

        public static void SendRequestForRetrieveItem(EntityCommandBuffer ecb, Entity sourceEntity, int2 targetCell, InventoryItem itemType)
        {
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new StorageRequest
            {
                GridCell = targetCell,
                RequestType = StorageRequestType.Withdraw,
                RequesterEntity = sourceEntity,
                ItemType = itemType
            });
        }
    }
}