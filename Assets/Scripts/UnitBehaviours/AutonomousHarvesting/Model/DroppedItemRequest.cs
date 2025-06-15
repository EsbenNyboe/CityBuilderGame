using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting.Model
{
    public struct DroppedItemRequest : IComponentData
    {
        public Entity RequesterEntity;
        public Entity DroppedItemEntity;
    }
}