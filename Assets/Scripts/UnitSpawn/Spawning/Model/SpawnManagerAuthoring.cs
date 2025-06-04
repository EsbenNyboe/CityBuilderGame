using Unity.Entities;
using UnityEngine;

namespace UnitSpawn.Spawning
{
    public struct SpawnManager : IComponentData
    {
        public Entity VillagerPrefab;
        public Entity BoarPrefab;
        public Entity TreePrefab;
        public Entity BedPrefab;
        public Entity StoragePrefab;
        public Entity HousePrefab;
        public Entity BonfirePrefab;
    }

    public class SpawnManagerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _villagerPrefab;
        [SerializeField] private GameObject _boarPrefab;
        [SerializeField] private GameObject _treePrefab;
        [SerializeField] private GameObject _bedPrefab;
        [SerializeField] private GameObject _storagePrefab;
        [SerializeField] private GameObject _housePrefab;
        [SerializeField] private GameObject _bonfirePrefab;

        public class SpawnManagerBaker : Baker<SpawnManagerAuthoring>
        {
            public override void Bake(SpawnManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnManager
                {
                    VillagerPrefab = GetEntity(authoring._villagerPrefab, TransformUsageFlags.Dynamic),
                    BoarPrefab = GetEntity(authoring._boarPrefab, TransformUsageFlags.Dynamic),
                    TreePrefab = GetEntity(authoring._treePrefab, TransformUsageFlags.Dynamic),
                    StoragePrefab = GetEntity(authoring._storagePrefab, TransformUsageFlags.Dynamic),
                    HousePrefab = GetEntity(authoring._housePrefab, TransformUsageFlags.Dynamic),
                    BedPrefab = GetEntity(authoring._bedPrefab, TransformUsageFlags.Dynamic),
                    BonfirePrefab = GetEntity(authoring._bonfirePrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}