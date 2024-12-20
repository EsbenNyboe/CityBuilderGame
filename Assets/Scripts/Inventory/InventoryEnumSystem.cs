using Unity.Entities;
using Utilities;

namespace Inventory
{
    public partial struct InventoryEnumSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new InventoryEnum
            {
                ItemEnumLength = EnumHelpers.GetMaxEnumValue<InventoryItem>() + 1
            });
        }
    }
}