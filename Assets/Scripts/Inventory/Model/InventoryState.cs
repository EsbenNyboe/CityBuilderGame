using System;
using Unity.Entities;

namespace Inventory
{
    public struct InventoryState : IComponentData
    {
        public InventoryItem CurrentItem;
        public float CurrentDurability;

        public void InsertItem(InventoryItem item)
        {
            CurrentItem = item;
            CurrentDurability = 1;
        }
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
        CookedMeat,
        RawMeat
    }
}