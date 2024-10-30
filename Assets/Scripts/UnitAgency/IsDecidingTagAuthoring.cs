using Unity.Entities;
using UnityEngine;

namespace UnitAgency
{
    public class IsDecidingTagAuthoring : MonoBehaviour
    {
        public class IsDecidingTagBaker : Baker<IsDecidingTagAuthoring>
        {
            public override void Bake(IsDecidingTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<IsDeciding>(entity);
            }
        }
    }
}