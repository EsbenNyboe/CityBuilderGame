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
    }
}