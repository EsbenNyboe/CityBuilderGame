using Unity.Entities;
using UnityEngine;

namespace UnitSpawn.Spawning
{
    public struct SpawnManager : IComponentData
    {
        public Entity UnitPrefab;
        public Entity BoarPrefab;
        public Entity StoragePrefab;
        public Entity TreePrefab;
    }

    public class SpawnManagerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private GameObject _boarPrefab;
        [SerializeField] private GameObject _storagePrefab;
        [SerializeField] private GameObject _treePrefab;

        public class SpawnManagerBaker : Baker<SpawnManagerAuthoring>
        {
            public override void Bake(SpawnManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnManager
                {
                    UnitPrefab = GetEntity(authoring._unitPrefab, TransformUsageFlags.Dynamic),
                    BoarPrefab = GetEntity(authoring._boarPrefab, TransformUsageFlags.Dynamic),
                    StoragePrefab = GetEntity(authoring._storagePrefab, TransformUsageFlags.Dynamic),
                    TreePrefab = GetEntity(authoring._treePrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}