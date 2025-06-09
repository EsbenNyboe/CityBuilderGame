using SystemGroups;
using UnitSpawn.SpawnedUnitNS;
using Unity.Entities;

namespace Inventory
{
    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    [UpdateBefore(typeof(SpawnedUnitSystem))]
    public partial struct InventoryStateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (inventoryState, spawnedUnit) in SystemAPI.Query<RefRW<InventoryState>, RefRO<SpawnedUnit>>())
            {
                inventoryState.ValueRW.CurrentItem = InventoryItem.RawMeat;
                inventoryState.ValueRW.CurrentDurability = 1;
            }

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