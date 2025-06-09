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
        }
    }
}