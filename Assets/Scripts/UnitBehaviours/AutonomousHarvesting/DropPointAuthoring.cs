using UnitBehaviours.Targeting;
using Unity.Entities;
using UnityEngine;

public class DropPointAuthoring : MonoBehaviour
{
    public class Baker : Baker<DropPointAuthoring>
    {
        public override void Bake(DropPointAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent<DropPoint>(entity);
            AddComponent<GridEntity>(entity);
            AddComponent<QuadrantEntity>(entity);
        }
    }
}

public struct DropPoint : IComponentData
{
}