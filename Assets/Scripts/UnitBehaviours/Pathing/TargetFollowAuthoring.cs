using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct TargetFollow : IComponentData
    {
        public Entity Target;
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