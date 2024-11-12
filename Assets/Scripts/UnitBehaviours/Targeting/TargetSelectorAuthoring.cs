using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct TargetSelector : IComponentData
    {
    }

    public class TargetSelectorAuthoring : MonoBehaviour
    {
        public class TargetSelectorBaker : Baker<TargetSelectorAuthoring>
        {
            public override void Bake(TargetSelectorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<TargetSelector>(entity);
            }
        }
    }
}