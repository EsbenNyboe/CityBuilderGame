using Unity.Entities;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are standing around and waiting for someone to talk to.
    /// </summary>
    public struct IsTalkative : IComponentData
    {
        public float Patience;
    }
}