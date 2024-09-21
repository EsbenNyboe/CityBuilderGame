using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class SpawnAtRandomPositionSystem : SystemBase
{
    public event EventHandler OnShoot;

    protected override void OnCreate()
    {
        RequireForUpdate<Player>();
    }

    protected override void OnUpdate()
    {
        return;
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        var spawnCubesConfig = SystemAPI.GetSingleton<SpawnCubesConfig>();

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);


        for (var i = 0; i < spawnCubesConfig.AmountToSpawn; i++)
        {
            GetRandomPosition(out var x, out var y);
            ValidateWalkableTargetPosition(ref x, ref y);
            var position = new float3(x, y, 0);

            var spawnedEntity = entityCommandBuffer.Instantiate(spawnCubesConfig.ObjectToSpawn);
            entityCommandBuffer.SetComponent(spawnedEntity, new LocalTransform
            {
                Position = position,
                Scale = 1f,
                Rotation = quaternion.identity
            });
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void ValidateWalkableTargetPosition(ref int endX, ref int endY)
    {
        var maxAttempts = 10;
        var currentAttempt = 0;
        while (currentAttempt < maxAttempts)
        {
            currentAttempt++;

            if (GridSetup.Instance.PathGrid.GetGridObject(endX, endY).IsWalkable())
            {
                return;
            }

            GetRandomPosition(out endX, out endY);
        }

        Debug.Log("Could not find walkable path");
    }

    private void GetRandomPosition(out int x, out int y)
    {
        x = Random.Range(0, GridSetup.Instance.PathGrid.GetWidth() - 1) ;
        y = Random.Range(0, GridSetup.Instance.PathGrid.GetHeight() - 1) ;
    }
}