using Unity.Entities;
using UnityEngine;

namespace UnitSpawn.Spawning
{
    public struct SpawnManager : IComponentData
    {
        public Entity UnitPrefab;
        public Entity BoarPrefab;
        public Entity TreePrefab;
        public Entity StoragePrefab;
        public Entity HousePrefab;
    }

    public class SpawnManagerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private GameObject _boarPrefab;
        [SerializeField] private GameObject _treePrefab;
        [SerializeField] private GameObject _storagePrefab;
        [SerializeField] private GameObject _housePrefab;

        public class SpawnManagerBaker : Baker<SpawnManagerAuthoring>
        {
            public override void Bake(SpawnManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnManager
                {
                    UnitPrefab = GetEntity(authoring._unitPrefab, TransformUsageFlags.Dynamic),
                    BoarPrefab = GetEntity(authoring._boarPrefab, TransformUsageFlags.Dynamic),
                    TreePrefab = GetEntity(authoring._treePrefab, TransformUsageFlags.Dynamic),
                    StoragePrefab = GetEntity(authoring._storagePrefab, TransformUsageFlags.Dynamic),
                    HousePrefab = GetEntity(authoring._housePrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}