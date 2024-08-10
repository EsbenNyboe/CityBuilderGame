using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = UnityEngine.Random;

public partial class SpawnCubesSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnCubesConfig>();
    }

    protected override void OnUpdate()
    {
        Enabled = false;

        var spawnCubesConfig = SystemAPI.GetSingleton<SpawnCubesConfig>();

        for (var i = 0; i < spawnCubesConfig.AmountToSpawn; i++)
        {
            var spawnedEntity = EntityManager.Instantiate(spawnCubesConfig.ObjectToSpawn);
            EntityManager.SetComponentData(spawnedEntity, new LocalTransform
            {
                Position = new float3
                {
                    x = Random.Range(-10f,
                        5f),
                    y = 0.6f,
                    z = Random.Range(-10f,
                        5f)
                },
                Scale = 1f,
                Rotation = quaternion.identity
            });
        }
    }
}