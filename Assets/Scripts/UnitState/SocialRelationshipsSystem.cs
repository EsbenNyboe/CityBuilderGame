using UnitBehaviours;
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
                     SystemAPI.Query<RefRO<SocialRelationships>>().WithDisabled<IsAlive>())
            {
                var relationships = socialRelationships.ValueRO.Relationships;
                if (relationships.IsCreated)
                {
                    relationships.Dispose();
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

            var spawnedUnitJobs = new SetupSpawnedUnitRelationshipsJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                ExistingUnits = existingUnits,
                SpawnedUnits = spawnedUnits
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