using UnitBehaviours;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnitState
{
    public struct SocialRelationships : IComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
        public Entity AnnoyingDude;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        private const int InitialCapacity = 100;
        private const float EvaluationInterval = 0.1f;
        private const float MinimumFondness = -2f;
        private const float MaximumFondness = 2f;
        private const float NeutralizationFactor = 0.01f;

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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();
            var existingUnits = _existingUnitsQuery.ToEntityArray(Allocator.TempJob);
            // EVALUATE EXISTING SOCIAL RELATIONSHIPS
            EvaluateExistingRelationships(ref state, existingUnits, socialDynamicsManager);

            // SETUP NEW SOCIAL RELATIONSHIPS
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            SetupNewRelationships(ref state, ecb, existingUnits);

            existingUnits.Dispose();
        }

        private void EvaluateExistingRelationships(ref SystemState state, NativeArray<Entity> existingUnits,
            SocialDynamicsManager socialDynamicsManager)
        {
            var annoyingDudeJob = new EvaluateAnnoyingDudeJob
            {
                ExistingUnits = existingUnits,
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>(),
                ThresholdForBecomingAnnoying = socialDynamicsManager.ThresholdForBecomingAnnoying
            }.Schedule(existingUnits.Length, 10);
            annoyingDudeJob.Complete();
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var timeSinceLastEvaluation = currentTime - _timeOfLastEvaluation;
            if (timeSinceLastEvaluation < EvaluationInterval)
            {
                return;
            }

            _timeOfLastEvaluation = currentTime;
            var evaluateAllRelationshipsJob = new EvaluateAllRelationshipsJob
            {
                ExistingUnits = existingUnits,
                NeutralizationAmount = NeutralizationFactor * timeSinceLastEvaluation,
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>()
            }.Schedule(existingUnits.Length, 10);
            evaluateAllRelationshipsJob.Complete();
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
        private struct EvaluateAllRelationshipsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> ExistingUnits;
            [ReadOnly] public float NeutralizationAmount;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            public void Execute(int index)
            {
                var socialRelationships = SocialRelationshipsLookup[ExistingUnits[index]];
                var relationships = socialRelationships.Relationships;

                foreach (var unit in ExistingUnits)
                {
                    var fondness = relationships[unit];

                    // Slowly forget whatever feelings you have for towards this person:
                    switch (fondness)
                    {
                        case > 0:
                            fondness -= NeutralizationAmount;
                            break;
                        case < 0:
                            fondness += NeutralizationAmount;
                            break;
                    }

                    // If you're beyond the actual range of emotion, you need to chill the fuck out:
                    fondness = math.clamp(fondness, MinimumFondness, MaximumFondness);
                    relationships[unit] = fondness;
                }

                socialRelationships.Relationships = relationships;
                SocialRelationshipsLookup[ExistingUnits[index]] = socialRelationships;
            }
        }

        [BurstCompile]
        private struct EvaluateAnnoyingDudeJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> ExistingUnits;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            [ReadOnly] public float ThresholdForBecomingAnnoying;

            public void Execute(int index)
            {
                var socialRelationships = SocialRelationshipsLookup[ExistingUnits[index]];

                var relationships = socialRelationships.Relationships;
                var annoyingDude = socialRelationships.AnnoyingDude;
                if (annoyingDude != Entity.Null && relationships[annoyingDude] >= ThresholdForBecomingAnnoying)
                {
                    // He's not annoying anymore
                    socialRelationships.AnnoyingDude = Entity.Null;
                    SocialRelationshipsLookup[ExistingUnits[index]] = socialRelationships;
                }
            }
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