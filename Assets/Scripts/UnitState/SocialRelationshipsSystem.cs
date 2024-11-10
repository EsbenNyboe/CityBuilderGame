using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UnitState
{
    public struct SocialRelationships : ICleanupComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
    }

    [UpdateInGroup(typeof(SpawningSystemGroup))]
    [UpdateAfter(typeof(SpawnManagerSystem))]
    [UpdateAfter(typeof(SpawnUnitsSystem))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        private const int InitialCapacity = 100;

        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (socialRelationships, entity) in SystemAPI.Query<RefRW<SocialRelationships>>()
                         .WithNone<LocalTransform>().WithEntityAccess())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
                ecb.RemoveComponent<SocialRelationships>(entity);
            }

            foreach (var (spawnedUnit, entity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                var relationships = new NativeHashMap<Entity, float>(InitialCapacity, Allocator.Persistent);
                var socialRelationships = new SocialRelationships
                {
                    Relationships = relationships
                };
                ecb.AddComponent(entity, socialRelationships);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}