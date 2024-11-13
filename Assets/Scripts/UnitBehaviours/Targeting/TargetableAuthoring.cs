using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct Targetable : IComponentData
    {
    }

    public class TargetableAuthoring : MonoBehaviour
    {
        public class TargetableBaker : Baker<TargetableAuthoring>
        {
            public override void Bake(TargetableAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<Targetable>(entity);
            }
        }
    }
}