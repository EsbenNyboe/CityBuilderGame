using Unity.Entities;
using Utilities;

namespace UnitState
{
    public struct InventoryEnum : IComponentData
    {
        public int ItemEnumLength;
    }

    public partial struct InventoryEnumSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new InventoryEnum
            {
                ItemEnumLength = EnumHelpers.GetMaxEnumValue<InventoryItem>()
            });
        }
    }
}