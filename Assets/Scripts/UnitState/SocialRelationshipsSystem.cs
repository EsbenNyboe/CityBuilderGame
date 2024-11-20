using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace UnitState
{
    public struct SocialRelationships : IComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        private const int InitialCapacity = 100;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // SETUP NEW SOCIAL RELATIONSHIPS
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, spawnedEntity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                var relationships = new NativeHashMap<Entity, float>(InitialCapacity, Allocator.Persistent);
                foreach (var (existingSocialRelationships, existingEntity) in SystemAPI
                             .Query<RefRW<SocialRelationships>>().WithEntityAccess())
                {
                    relationships.Add(existingEntity, 0);
                    existingSocialRelationships.ValueRW.Relationships.Add(spawnedEntity, 0);
                }

                foreach (var (_, otherSpawnedEntity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
                {
                    relationships.Add(otherSpawnedEntity, 0);
                }

                var socialRelationships = new SocialRelationships
                {
                    Relationships = relationships
                };
                ecb.AddComponent(spawnedEntity, socialRelationships);
            }
        }
    }
}