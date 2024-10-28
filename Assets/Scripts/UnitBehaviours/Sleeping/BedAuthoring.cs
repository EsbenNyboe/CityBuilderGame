using Unity.Entities;
using UnityEngine;

public struct Bed : IComponentData
{
}

public class BedAuthoring : MonoBehaviour
{
    public class BedBaker : Baker<BedAuthoring>
    {
        public override void Bake(BedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Bed>(entity);
        }
    }
}