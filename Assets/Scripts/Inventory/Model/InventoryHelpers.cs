using Grid;
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
                Item = inventory.CurrentItem
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
                Item = inventory.CurrentItem
            });
            ecbParallelWriter.AddComponent(i, droppedItemEntity, new LocalTransform
            {
                Position = position,
                Scale = 1,
                Rotation = quaternion.identity
            });
            inventory.CurrentItem = InventoryItem.None;
        }

        public static bool TryDropItemInStorage(EntityCommandBuffer ecb, ref GridManager gridManager,
            ref InventoryState inventory, float3 position)
        {
            var droppedItemEntity = ecb.CreateEntity();
            ecb.AddComponent(droppedItemEntity, new DroppedItem
            {
                Item = inventory.CurrentItem
            });
            ecb.AddComponent(droppedItemEntity, new LocalTransform
            {
                Position = position,
                Scale = 1,
                Rotation = quaternion.identity
            });
            inventory.CurrentItem = InventoryItem.None;

            if (!gridManager.TryGetDropPointEntity(position, out var dropPointEntity))
            {
                return false;
            }

            var storageItemCount = gridManager.GetStorageItemCount(position);
            if (storageItemCount >= 5)
            {
                return false;
            }

            gridManager.SetItemCount(position, storageItemCount + 1);

            return true;
        }
    }
}