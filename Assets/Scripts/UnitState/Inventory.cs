using System;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace UnitState
{
    public struct Inventory : IComponentData
    {
        public InventoryItem CurrentItem;
    }

    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood
    }
}