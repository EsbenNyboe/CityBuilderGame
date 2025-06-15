using Inventory;
using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsSeekingFilledStorage : IComponentData
    {
        public InventoryItem ItemType;
    }
}