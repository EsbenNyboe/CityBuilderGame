using Unity.Entities;

namespace UnitBehaviours.Targeting
{
    public struct TargetFollow : IComponentData
    {
        public Entity Target;
        public float DesiredRange;
        public float CurrentDistanceToTarget;

        public readonly bool TryGetTarget(out Entity target)
        {
            target = Target;
            return target != Entity.Null;
        }
    }
}