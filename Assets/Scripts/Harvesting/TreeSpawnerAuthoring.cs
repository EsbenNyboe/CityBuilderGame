using Unity.Entities;
using UnityEngine;

public class TreeSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _objectToSpawn;

    public class Baker : Baker<TreeSpawnerAuthoring>
    {
        public override void Bake(TreeSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TreeSpawner
            {
                ObjectToSpawn = GetEntity(authoring._objectToSpawn, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct TreeSpawner : IComponentData
{
    public Entity ObjectToSpawn;
}