using Unity.Entities;
using UnityEngine;

public class SpawnCubesConfigAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _objectToSpawn;

    [SerializeField] private int _amountToSpawn;

    public class Baker : Baker<SpawnCubesConfigAuthoring>
    {
        public override void Bake(SpawnCubesConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SpawnCubesConfig
            {
                ObjectToSpawn = GetEntity(authoring._objectToSpawn, TransformUsageFlags.Dynamic),
                AmountToSpawn = authoring._amountToSpawn
            });
        }
    }
}

public struct SpawnCubesConfig : IComponentData
{
    public Entity ObjectToSpawn;
    public int AmountToSpawn;
}