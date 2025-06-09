using System;
using Unity.Entities;

namespace Inventory
{
    public struct InventoryState : IComponentData
    {
        public InventoryItem CurrentItem;
    }

    public struct DroppedItem : IComponentData
    {
        public InventoryItem Item;
    }

    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood,
        CookedMeat
    }
}