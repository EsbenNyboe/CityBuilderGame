using Unity.Entities;

namespace Inventory
{
    public partial struct InventoryStateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // var inventoryCaches = new NativeList
            foreach (var inventory in SystemAPI.Query<RefRW<InventoryState>>())
            {
                if (inventory.ValueRO.CurrentItem == InventoryItem.None)
                {
                    inventory.ValueRW.CurrentDurability = 0;
                }
            }
        }
    }
}