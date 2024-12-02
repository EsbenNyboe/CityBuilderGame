using System;
using Unity.Entities;
using Unity.Mathematics;

namespace UnitState
{
    public struct Inventory : IComponentData
    {
        public InventoryItem CurrentItem;
    }

    public struct DroppedItem : IComponentData
    {
        public InventoryItem Item;
        public float2 Position;
    }

    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood
    }
}