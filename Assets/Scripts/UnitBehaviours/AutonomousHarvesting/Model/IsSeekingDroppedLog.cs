using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsSeekingDroppedLog : IComponentData
    {
        public bool HasStartedMoving;
    }
}