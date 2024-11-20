using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace UnitState
{
    public struct SocialRelationships : IComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        private EntityQuery _existingUnitsQuery;
        private EntityQuery _spawnedUnitsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            _existingUnitsQuery = state.GetEntityQuery(ComponentType.ReadWrite(typeof(SocialRelationships)));
            _spawnedUnitsQuery = state.GetEntityQuery(ComponentType.ReadOnly(typeof(SpawnedUnit)));
        }

        private const int InitialCapacity = 100;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // SETUP NEW SOCIAL RELATIONSHIPS
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var existingUnits = _existingUnitsQuery.ToEntityArray(Allocator.TempJob);
            var spawnedUnits = _spawnedUnitsQuery.ToEntityArray(Allocator.TempJob);

            var spawnedUnitJobs = new SetupSpawnedUnitRelationshipsJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                ExistingUnits = existingUnits,
                SpawnedUnits = spawnedUnits
            }.Schedule(spawnedUnits.Length, 200);

            var existingUnitJobs = new UpdateExistingRelationshipsJob
            {
                ExistingUnits = existingUnits,
                SpawnedUnits = spawnedUnits,
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>()
            }.Schedule(existingUnits.Length, 200);

            spawnedUnitJobs.Complete();
            existingUnitJobs.Complete();

            existingUnits.Dispose();
            spawnedUnits.Dispose();
        }

        [BurstCompile]
        private struct UpdateExistingRelationshipsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> ExistingUnits;
            [ReadOnly] public NativeArray<Entity> SpawnedUnits;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            public void Execute(int index)
            {
                var socialRelationships = SocialRelationshipsLookup[ExistingUnits[index]];
                foreach (var spawnedUnit in SpawnedUnits)
                {
                    socialRelationships.Relationships.Add(spawnedUnit, 0);
                }

                SocialRelationshipsLookup[ExistingUnits[index]] = socialRelationships;
            }
        }

        [BurstCompile]
        private struct SetupSpawnedUnitRelationshipsJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
            [ReadOnly] public NativeArray<Entity> ExistingUnits;
            [ReadOnly] public NativeArray<Entity> SpawnedUnits;

            public void Execute(int index)
            {
                var relationships = new NativeHashMap<Entity, float>(InitialCapacity, Allocator.Persistent);

                foreach (var existingUnit in ExistingUnits)
                {
                    relationships.Add(existingUnit, 0);
                }

                foreach (var spawnedUnit in SpawnedUnits)
                {
                    relationships.Add(spawnedUnit, 0);
                }

                EcbParallelWriter.AddComponent(index, SpawnedUnits[index], new SocialRelationships
                {
                    Relationships = relationships
                });
            }
        }
    }
}