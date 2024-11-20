using System;
using Unity.Entities;

namespace UnitState
{
    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood
    }

    public struct Inventory : IComponentData
    {
        public InventoryItem CurrentItem;
    }
}