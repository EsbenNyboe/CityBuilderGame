using Unity.Entities;

namespace UnitBehaviours.Targeting
{
    public struct IsMurdering : IComponentData
    {
        public Entity Target;
    }
}