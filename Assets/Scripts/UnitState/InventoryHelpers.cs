using UnitBehaviours.Targeting;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitState
{
    public class InventoryHelpers
    {
        public static void DropItemOnGround(EntityCommandBuffer ecb,
            ref Inventory inventory, float3 position)
        {
            var droppedItemEntity = ecb.CreateEntity();
            ecb.AddComponent(droppedItemEntity, new DroppedItem
            {
                Item = inventory.CurrentItem
            });
            ecb.AddComponent<QuadrantEntity>(droppedItemEntity);
            ecb.AddComponent(droppedItemEntity, new LocalTransform
            {
                Position = position,
                Scale = 1,
                Rotation = quaternion.identity
            });
            inventory.CurrentItem = InventoryItem.None;
        }

        public static void DropItemOnGround(EntityCommandBuffer.ParallelWriter ecbParallelWriter, int i,
            ref Inventory inventory, float3 position)
        {
            var droppedItemEntity = ecbParallelWriter.CreateEntity(i);
            ecbParallelWriter.AddComponent(i, droppedItemEntity, new DroppedItem
            {
                Item = inventory.CurrentItem
            });
            ecbParallelWriter.AddComponent<QuadrantEntity>(i, droppedItemEntity);
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