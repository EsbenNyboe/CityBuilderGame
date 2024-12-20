using Unity.Entities;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are talking with someone, or trying to, at least!
    /// </summary>
    public struct IsTalking : IComponentData, IEnableableComponent
    {
    }
}