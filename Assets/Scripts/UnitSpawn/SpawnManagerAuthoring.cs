using Unity.Entities;
using UnityEngine;

public struct SpawnManager : IComponentData
{
    public Entity UnitPrefab;
    public Entity ZombiePrefab;
    public Entity DropPointPrefab;
}

public class SpawnManagerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _unitPrefab;
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private GameObject _dropPointPrefab;

    public class SpawnManagerBaker : Baker<SpawnManagerAuthoring>
    {
        public override void Bake(SpawnManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SpawnManager
            {
                UnitPrefab = GetEntity(authoring._unitPrefab, TransformUsageFlags.Dynamic),
                ZombiePrefab = GetEntity(authoring._zombiePrefab, TransformUsageFlags.Dynamic),
                DropPointPrefab = GetEntity(authoring._dropPointPrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}