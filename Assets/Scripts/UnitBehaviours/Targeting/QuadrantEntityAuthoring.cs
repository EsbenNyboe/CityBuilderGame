using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct QuadrantEntity : IComponentData
    {
    }

    public class QuadrantEntityAuthoring : MonoBehaviour
    {
        public class QuadrantEntityBaker : Baker<QuadrantEntityAuthoring>
        {
            public override void Bake(QuadrantEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<QuadrantEntity>(entity);
            }
        }
    }
}