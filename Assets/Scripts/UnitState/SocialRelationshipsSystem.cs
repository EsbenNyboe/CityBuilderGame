using UnitBehaviours;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace UnitState
{
    public struct SocialRelationships : ICleanupComponentData
    {
        public NativeParallelHashMap<Entity, float> Relationships;
        public float TimeOfLastEvaluation;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        private const int InitialCapacity = 100;

        private EntityQuery _existingUnitsQuery;
        private EntityQuery _spawnedUnitsQuery;
        private float _timeOfLastEvaluation;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            _existingUnitsQuery = state.GetEntityQuery(ComponentType.ReadWrite(typeof(SocialRelationships)));
            _spawnedUnitsQuery = state.GetEntityQuery(ComponentType.ReadOnly(typeof(SpawnedUnit)));
        }

        public void OnDestroy(ref SystemState state)
        {
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>())
            {
                if (socialRelationships.ValueRO.Relationships.IsCreated)
                {
                    socialRelationships.ValueRW.Relationships.Clear();
                    socialRelationships.ValueRW.Relationships.Dispose();
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var existingUnits = _existingUnitsQuery.ToEntityArray(Allocator.TempJob);

            // SETUP NEW SOCIAL RELATIONSHIPS
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            SetupNewRelationships(ref state, ecb, existingUnits);

            existingUnits.Dispose();
        }

        private void SetupNewRelationships(ref SystemState state, EntityCommandBuffer ecb,
            NativeArray<Entity> existingUnits)
        {
            var spawnedUnits = _spawnedUnitsQuery.ToEntityArray(Allocator.TempJob);

            var newSocialRelationships = new NativeArray<NativeParallelHashMap<Entity, float>>(spawnedUnits.Length, Allocator.TempJob);
            for (var i = 0; i < newSocialRelationships.Length; i++)
            {
                newSocialRelationships[i] =
                    new NativeParallelHashMap<Entity, float>(spawnedUnits.Length + existingUnits.Length, Allocator.Persistent);
            }

            var spawnedUnitJobs = new SetupSpawnedUnitRelationshipsJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                ExistingUnits = existingUnits,
                SpawnedUnits = spawnedUnits,
                NewSocialRelationships = newSocialRelationships
            }.Schedule(spawnedUnits.Length, 10);

            var existingUnitJobs = new UpdateExistingRelationshipsJob
            {
                ExistingUnits = existingUnits,
                SpawnedUnits = spawnedUnits,
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>()
            }.Schedule(existingUnits.Length, 10);

            spawnedUnitJobs.Complete();
            existingUnitJobs.Complete();
            spawnedUnits.Dispose();
            newSocialRelationships.Dispose();
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
                foreach (var spawnedUnit in SpawnedUnits)
                {
                    SocialRelationshipsLookup[ExistingUnits[index]].Relationships.Add(spawnedUnit, 0);
                }
            }
        }

        [BurstCompile]
        private struct SetupSpawnedUnitRelationshipsJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
            [ReadOnly] public NativeArray<Entity> ExistingUnits;
            [ReadOnly] public NativeArray<Entity> SpawnedUnits;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<NativeParallelHashMap<Entity, float>> NewSocialRelationships;

            public void Execute(int index)
            {
                var relationships = NewSocialRelationships[index];

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