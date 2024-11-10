using Unity.Entities;
using UnityEngine;

public struct SpawnedUnit : IComponentData
{
}

public class SpawnedUnitAuthoring : MonoBehaviour
{
    public class SpawnedUnitBaker : Baker<SpawnedUnitAuthoring>
    {
        public override void Bake(SpawnedUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<SpawnedUnit>(entity);
        }
    }
}