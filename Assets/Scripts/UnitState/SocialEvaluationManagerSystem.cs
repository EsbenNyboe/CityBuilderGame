using SystemGroups;
using UnitBehaviours.UnitManagers;
using UnitSpawn;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnitState
{
    public struct SocialEvaluationManager : IComponentData
    {
        public NativeQueue<Entity> SocialEvaluationQueue;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialEvaluationManagerSystem : ISystem
    {
        private EntityQuery _query;
        private const int MaxEvaluatorsPerFrame = 10;
        private const float MinimumFondness = -2f;
        private const float MaximumFondness = 2f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<SocialEvaluationManager>();
            state.EntityManager.CreateSingleton<SocialEvaluationManager>();
            SystemAPI.SetSingleton(new SocialEvaluationManager
            {
                SocialEvaluationQueue = new NativeQueue<Entity>(Allocator.Persistent)
            });
            _query = state.GetEntityQuery(ComponentType.ReadOnly<SocialRelationships>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var socialEvaluationManager = SystemAPI.GetSingleton<SocialEvaluationManager>();
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();

            var allEntities = _query.ToEntityArray(Allocator.TempJob);
            var socialEvaluationCount =
                math.min(socialEvaluationManager.SocialEvaluationQueue.Count, MaxEvaluatorsPerFrame);

            var entities = new NativeArray<Entity>(socialEvaluationCount, Allocator.TempJob);

            var currentSocialEvaluations = 0;
            while (currentSocialEvaluations < socialEvaluationCount)
            {
                var entityToEvaluate = socialEvaluationManager.SocialEvaluationQueue.Dequeue();
                entities[currentSocialEvaluations] = entityToEvaluate;
                currentSocialEvaluations++;
                socialEvaluationManager.SocialEvaluationQueue.Enqueue(entityToEvaluate);
            }

            var job = new SocialEvaluationJob
            {
                JobEntities = entities,
                AllEntities = allEntities,
                NeutralizationAmount = socialDynamicsManager.NeutralizationFactor,
                Time = (float)SystemAPI.Time.ElapsedTime,
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>()
            }.Schedule(entities.Length, 1);
            job.Complete();

            entities.Dispose();
            allEntities.Dispose();

            // Add spawned units to queue
            foreach (var (spawnedUnit, entity) in SystemAPI
                         .Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                socialEvaluationManager.SocialEvaluationQueue.Enqueue(entity);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            var socialEvaluationManager = SystemAPI.GetSingleton<SocialEvaluationManager>();
            socialEvaluationManager.SocialEvaluationQueue.Dispose();
        }

        [BurstCompile]
        private struct SocialEvaluationJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> JobEntities;
            [ReadOnly] public NativeArray<Entity> AllEntities;
            [ReadOnly] public float NeutralizationAmount;
            [ReadOnly] public float Time;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;


            public void Execute(int index)
            {
                var socialRelationships = SocialRelationshipsLookup[JobEntities[index]];
                var relationships = socialRelationships.Relationships;
                var timeSinceLastEvaluation = Time - socialRelationships.TimeOfLastEvaluation;

                for (var i = 0; i < AllEntities.Length; i++)
                {
                    var fondness = socialRelationships.Relationships[AllEntities[i]];

                    // Slowly forget whatever feelings you have for towards this person:
                    switch (fondness)
                    {
                        case > 0:
                            fondness -= NeutralizationAmount * timeSinceLastEvaluation;
                            if (fondness < 0)
                            {
                                fondness = 0;
                            }

                            break;
                        case < 0:
                            fondness += NeutralizationAmount * timeSinceLastEvaluation;
                            if (fondness > 0)
                            {
                                fondness = 0;
                            }

                            break;
                    }

                    // If you're beyond the actual range of emotion, you need to chill the fuck out:
                    fondness = math.clamp(fondness, MinimumFondness, MaximumFondness);
                    relationships[AllEntities[i]] = fondness;
                }

                socialRelationships.TimeOfLastEvaluation = Time;
                socialRelationships.Relationships = relationships;
                SocialRelationshipsLookup[JobEntities[index]] = socialRelationships;
            }
        }
    }
}