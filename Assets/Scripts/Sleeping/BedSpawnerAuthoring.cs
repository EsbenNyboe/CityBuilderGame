using Unity.Entities;
using UnityEngine;

public struct BedSpawner : IComponentData
{
    public Entity Prefab;
}

public class BedSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _bedPrefab;

    public class BedSpawnerBaker : Baker<BedSpawnerAuthoring>
    {
        public override void Bake(BedSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BedSpawner
            {
                Prefab = GetEntity(authoring._bedPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}