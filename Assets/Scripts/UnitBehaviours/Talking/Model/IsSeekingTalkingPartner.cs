using Unity.Entities;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are pathfinding to someone else, usually if we wanted to be social, but there was no one close by
    /// </summary>
    public struct IsSeekingTalkingPartner : IComponentData
    {
        public bool HasStartedMoving;
    }
}