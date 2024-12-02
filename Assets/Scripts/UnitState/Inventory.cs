﻿using System;
using Unity.Entities;

namespace UnitState
{
    public struct Inventory : IComponentData
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
        LogOfWood
    }
}