using Unity.Entities;
using UnityEngine;

namespace UnitState
{
    public struct IsAlive : IComponentData, IEnableableComponent
    {
    }

    public class IsAliveAuthoring : MonoBehaviour
    {
        public class IsAliveBaker : Baker<IsAliveAuthoring>
        {
            public override void Bake(IsAliveAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<IsAlive>(entity);
                SetComponentEnabled<IsAlive>(entity, true);
            }
        }
    }
}