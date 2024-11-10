using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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
            CleanupDeletedUnits(ref state);

            // SETUP NEW SOCIAL RELATIONSHIPS
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (_, spawnedEntity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                var relationships = new NativeHashMap<Entity, float>(InitialCapacity, Allocator.Persistent);
                foreach (var (existingSocialRelationships, existingEntity) in SystemAPI
                             .Query<RefRW<SocialRelationships>>().WithEntityAccess())
                {
                    relationships.Add(existingEntity, 0);
                    existingSocialRelationships.ValueRW.Relationships.Add(spawnedEntity, 0);
                    // TODO: Remember to add spawned entities to spawned social relationships.
                }

                var socialRelationships = new SocialRelationships
                {
                    Relationships = relationships
                };
                ecb.AddComponent(spawnedEntity, socialRelationships);
            }

            ecb.Playback(state.EntityManager);
        }

        private void CleanupDeletedUnits(ref SystemState state)
        {
            // CLEANUP SOCIAL RELATIONSHIPS
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (socialRelationships, entity) in SystemAPI.Query<RefRW<SocialRelationships>>()
                         .WithNone<LocalTransform>().WithEntityAccess())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
                ecb.RemoveComponent<SocialRelationships>(entity);

                // TODO: Remove self from other relationships
            }

            ecb.Playback(state.EntityManager);
        }
    }
    
    public partial struct SocialRelationshipsDebugSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (localTransform, socialRelationships) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<SocialRelationships>>().WithAll<UnitSelection>())
            {
                foreach (var relationship in socialRelationships.ValueRO.Relationships)
                {
                    var otherPosition = SystemAPI.GetComponent<LocalTransform>(relationship.Key).Position;
                    Debug.DrawLine(localTransform.ValueRO.Position, otherPosition, Color.red);
                }
            }
        }
    }
}