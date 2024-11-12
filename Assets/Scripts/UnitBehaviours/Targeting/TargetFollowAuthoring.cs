using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetFollow : IComponentData
    {
        public Entity Target;
        public float CurrentDistanceToTarget;
    }

    public partial struct TargetFollow
    {
        public readonly bool TryGetTarget(out Entity target)
        {
            target = Target;
            return target != Entity.Null;
        }
    }

    public class TargetFollowAuthoring : MonoBehaviour
    {
        public class TargetFollowBaker : Baker<TargetFollowAuthoring>
        {
            public override void Bake(TargetFollowAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<TargetFollow>(entity);
            }
        }
    }
}