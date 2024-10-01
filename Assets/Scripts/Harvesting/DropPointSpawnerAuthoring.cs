using Unity.Entities;
using UnityEngine;

public class DropPointSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _objectToSpawn;

    public class Baker : Baker<DropPointSpawnerAuthoring>
    {
        public override void Bake(DropPointSpawnerAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new DropPointSpawner
            {
                ObjectToSpawn = GetEntity(authoring._objectToSpawn, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct DropPointSpawner : IComponentData
{
    public Entity ObjectToSpawn;
}