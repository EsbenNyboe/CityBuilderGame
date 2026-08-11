using System;
using Unity.Entities;

namespace Inventory
{
    public struct InventoryState : IComponentData
    {
        public InventoryItem CurrentItem;
        public float CurrentDurability;

        public InventoryState(InventoryItem inventoryItem, float durability)
        {
            CurrentItem = inventoryItem;
            CurrentDurability = durability;
        }

        public void InsertItem(InventoryItem item)
        {
            CurrentItem = item;
            CurrentDurability = 1;
        }
    }

    public struct DroppedItem : IComponentData
    {
        public InventoryItem ItemType;
    }

    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood,
        RawMeat,
        CookedMeat,
        BunchOfBerries
    }
}