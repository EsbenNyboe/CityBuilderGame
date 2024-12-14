using GridEntityNS;
using UnitBehaviours.Targeting;
using Unity.Entities;
using UnityEngine;

public class TreeAuthoring : MonoBehaviour
{
    public class TreeBaker : Baker<TreeAuthoring>
    {
        public override void Bake(TreeAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent<Tree>(entity);
            AddComponent<GridEntity>(entity);
            AddComponent<QuadrantEntity>(entity);
        }
    }
}

public struct Tree : IComponentData
{
}