using Unity.Entities;

namespace Inventory
{
    public struct Storage : IComponentData
    {
        public int Capacity;
        public int CurrentItems;
    }
}