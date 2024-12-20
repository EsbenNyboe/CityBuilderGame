using Unity.Entities;

namespace UnitBehaviours.Targeting
{
    public struct IsThrowingSpear : IComponentData
    {
        public Entity Target;
    }
}