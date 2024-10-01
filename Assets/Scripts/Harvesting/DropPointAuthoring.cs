using Unity.Entities;
using UnityEngine;

public class DropPointAuthoring : MonoBehaviour
{
    public class Baker : Baker<DropPointAuthoring>
    {
        public override void Bake(DropPointAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new DropPoint());
        }
    }
}

public struct DropPoint : IComponentData
{
}