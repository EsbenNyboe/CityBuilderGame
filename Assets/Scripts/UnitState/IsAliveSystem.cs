using Events;
using UnitBehaviours;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

namespace UnitState
{
    public struct IsAlive : IComponentData, IEnableableComponent
    {
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial struct IsAliveSystem : ISystem
    {
        private EntityQuery _aliveVillagerQuery;
        private EntityQuery _deadVillagerQuery;
        private EntityQuery _deadBoarQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<SocialEvaluationManager>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<IsAlive>();

            _deadBoarQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Boar),
                    typeof(LocalTransform)
                },
                Disabled = new ComponentType[]
                {
                    typeof(IsAlive)
                }
            });
            _deadVillagerQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Villager),
                    typeof(LocalTransform)
                },
                Disabled = new ComponentType[]
                {
                    typeof(IsAlive)
                }
            });
            _aliveVillagerQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Villager),
                    typeof(SocialRelationships),
                    typeof(IsAlive)
                }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            if (_deadVillagerQuery.CalculateEntityCount() <= 0)
            {
                return;
            }

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var deadVillagers = _deadVillagerQuery.ToEntityArray(Allocator.TempJob);
            using var invalidSocialEvents = new NativeList<Entity>(Allocator.Temp);
            using var invalidSocialEventsWithVictim = new NativeList<Entity>(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var profilerA = new ProfilerMarker("A");
            profilerA.Begin();
            // Cleanup social events
            foreach (var (socialEvent, entity) in SystemAPI.Query<RefRO<SocialEvent>>().WithEntityAccess())
            {
                for (var i = 0; i < deadVillagers.Length; i++)
                {
                    if (socialEvent.ValueRO.Perpetrator == deadVillagers[i])
                    {
                        invalidSocialEvents.Add(entity);
                    }
                }
            }

            // Cleanup social events with victim
            foreach (var (socialEventWithVictim, entity) in SystemAPI.Query<RefRO<SocialEventWithVictim>>()
                         .WithEntityAccess())
            {
                for (var i = 0; i < deadVillagers.Length; i++)
                {
                    if (socialEventWithVictim.ValueRO.Perpetrator == deadVillagers[i] ||
                        socialEventWithVictim.ValueRO.Victim == deadVillagers[i])
                    {
                        invalidSocialEventsWithVictim.Add(entity);
                    }
                }
            }

            profilerA.End();

            var profilerB = new ProfilerMarker("B");
            profilerB.Begin();
            // Play death effect
            new PlayDeathEffectJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                UnitType = UnitType.Villager
            }.ScheduleParallel(_deadVillagerQuery, state.Dependency).Complete();

            new PlayDeathEffectJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                UnitType = UnitType.Boar
            }.ScheduleParallel(_deadBoarQuery, state.Dependency).Complete();
            profilerB.End();
            var profilerC = new ProfilerMarker("C");
            profilerC.Begin();

            // Cleanup grid
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>()
                         .WithEntityAccess())
            {
                var position = localTransform.ValueRO.Position;
                gridManager.OnUnitDestroyed(entity, position);
            }

            SystemAPI.SetSingleton(gridManager);

            profilerC.End();
            var profilerD = new ProfilerMarker("D");
            profilerD.Begin();

            // Cleanup dead units relationships
            foreach (var (socialRelationships, entity) in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithDisabled<IsAlive>().WithEntityAccess())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
                ecb.RemoveComponent<SocialRelationships>(entity);
            }

            profilerD.End();
            var profilerE = new ProfilerMarker("E");
            profilerE.Begin();

            var relationshipsOfAllLivingVillagers = _aliveVillagerQuery.ToComponentDataArray<SocialRelationships>(Allocator.TempJob);
            new CleanupAliveVillagersRelationshipsJob
            {
                DeadVillagers = deadVillagers,
                RelationshipsOfAllLivingVillagers = relationshipsOfAllLivingVillagers
            }.Schedule(relationshipsOfAllLivingVillagers.Length, 1).Complete();

            relationshipsOfAllLivingVillagers.Dispose();

            profilerE.End();

            var profilerF = new ProfilerMarker("F");
            profilerF.Begin();

            // Cleanup SocialEvaluation-queue
            var socialEvaluationManager = SystemAPI.GetSingleton<SocialEvaluationManager>();
            var queueLength = socialEvaluationManager.SocialEvaluationQueue.Count;
            var queueIndex = 0;
            while (queueIndex < queueLength)
            {
                queueIndex++;
                var socialEvaluationEntry = socialEvaluationManager.SocialEvaluationQueue.Dequeue();
                var isDead = false;
                foreach (var deadUnit in deadVillagers)
                {
                    if (deadUnit == socialEvaluationEntry)
                    {
                        isDead = true;
                        break;
                    }
                }

                if (!isDead)
                {
                    socialEvaluationManager.SocialEvaluationQueue.Enqueue(socialEvaluationEntry);
                }
            }

            profilerF.End();

            var profilerG = new ProfilerMarker("G");
            profilerG.Begin();

            new CleanupTargetFollowJob
            {
                DeadVillagers = deadVillagers
            }.ScheduleParallel();

            new CleanupIsMurderingJob
            {
                DeadVillagers = deadVillagers
            }.ScheduleParallel();
            profilerG.End();

            var profilerH = new ProfilerMarker("H");
            profilerH.Begin();

            // Destroy dead units
            state.EntityManager.DestroyEntity(deadVillagers);
            deadVillagers.Dispose();
            state.EntityManager.DestroyEntity(invalidSocialEvents.AsArray());
            state.EntityManager.DestroyEntity(invalidSocialEventsWithVictim.AsArray());
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            profilerH.End();
        }
    }

    [BurstCompile]
    public struct CleanupAliveVillagersRelationshipsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> DeadVillagers;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<SocialRelationships> RelationshipsOfAllLivingVillagers;

        public void Execute(int index)
        {
            foreach (var deadUnit in DeadVillagers)
            {
                RelationshipsOfAllLivingVillagers[index].Relationships.Remove(deadUnit);
            }
        }
    }

    [BurstCompile]
    public partial struct CleanupIsMurderingJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> DeadVillagers;

        public void Execute(ref IsMurdering isMurdering)
        {
            foreach (var deadUnit in DeadVillagers)
            {
                if (deadUnit == isMurdering.Target)
                {
                    isMurdering.Target = Entity.Null;
                }
            }
        }
    }

    [BurstCompile]
    public partial struct CleanupTargetFollowJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> DeadVillagers;

        public void Execute(ref TargetFollow targetFollow)
        {
            foreach (var deadUnit in DeadVillagers)
            {
                if (deadUnit == targetFollow.Target)
                {
                    targetFollow.Target = Entity.Null;
                    targetFollow.CurrentDistanceToTarget = math.INFINITY;
                    targetFollow.DesiredRange = 0;
                }
            }
        }
    }

    [BurstCompile]
    public partial struct PlayDeathEffectJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
        [ReadOnly] public UnitType UnitType;

        public void Execute(in LocalTransform localTransform, [EntityIndexInChunk] int entityIndexInChunk)
        {
            EcbParallelWriter.AddComponent(entityIndexInChunk, EcbParallelWriter.CreateEntity(entityIndexInChunk),
                new DeathEvent
                {
                    Position = localTransform.Position,
                    TargetType = UnitType
                });
        }
    }
}