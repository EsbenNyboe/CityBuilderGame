using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class PlayerShootingSystem : SystemBase
{
    public event EventHandler OnShoot;

    protected override void OnCreate()
    {
        RequireForUpdate<Player>();
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            EntityManager.SetComponentEnabled<Stunned>(playerEntity, true);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            EntityManager.SetComponentEnabled<Stunned>(playerEntity, false);
        }

        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        var spawnCubesConfig = SystemAPI.GetSingleton<SpawnCubesConfig>();

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Player>().WithDisabled<Stunned>()
                     .WithEntityAccess())
        {
            // var spawnedEntity = EntityManager.Instantiate(spawnCubesConfig.ObjectToSpawn);
            // EntityManager.SetComponentData(spawnedEntity, new LocalTransform
            var spawnedEntity = entityCommandBuffer.Instantiate(spawnCubesConfig.ObjectToSpawn);
            entityCommandBuffer.SetComponent(spawnedEntity, new LocalTransform
            {
                Position = localTransform.ValueRO.Position,
                Scale = 1f,
                Rotation = quaternion.identity
            });

            OnShoot?.Invoke(entity, EventArgs.Empty);

            PlayerShootManager.Instance.PlayerShoot(localTransform.ValueRO.Position);
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}