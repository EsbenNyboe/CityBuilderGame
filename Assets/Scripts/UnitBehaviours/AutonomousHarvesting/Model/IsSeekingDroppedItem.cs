using Inventory;
using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsSeekingDroppedItem : IComponentData
    {
        public InventoryItem ItemType;
        public bool HasStartedMoving;
    }
}