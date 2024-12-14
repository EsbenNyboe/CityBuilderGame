using Unity.Entities;
using UnityEngine;

public struct SpawnManager : IComponentData
{
    public Entity UnitPrefab;
    public Entity BoarPrefab;
    public Entity DropPointPrefab;
    public Entity TreePrefab;
}

public class SpawnManagerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _unitPrefab;
    [SerializeField] private GameObject _boarPrefab;
    [SerializeField] private GameObject _dropPointPrefab;
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
                DropPointPrefab = GetEntity(authoring._dropPointPrefab, TransformUsageFlags.Dynamic),
                TreePrefab = GetEntity(authoring._treePrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}