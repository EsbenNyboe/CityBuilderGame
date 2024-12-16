using Events;
using Grid;
using UnitBehaviours.Tags;
using UnitBehaviours.Targeting;
using UnitState.AliveState;
using UnitState.SocialLogic;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitState.AliveLogic
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(SocialEventSystem))]
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
            if (_deadVillagerQuery.CalculateEntityCount() <= 0 && _deadBoarQuery.CalculateEntityCount() <= 0)
            {
                return;
            }

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var deadVillagers = _deadVillagerQuery.ToEntityArray(Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

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

            // Cleanup grid
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>()
                         .WithEntityAccess())
            {
                var position = localTransform.ValueRO.Position;
                gridManager.OnUnitDestroyed(entity, position);
            }

            SystemAPI.SetSingleton(gridManager);

            // Cleanup dead units relationships
            foreach (var (socialRelationships, entity) in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithDisabled<IsAlive>().WithEntityAccess())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
                ecb.RemoveComponent<SocialRelationships>(entity);
            }

            var relationshipsOfAllLivingVillagers = _aliveVillagerQuery.ToComponentDataArray<SocialRelationships>(Allocator.TempJob);
            new CleanupAliveVillagersRelationshipsJob
            {
                DeadVillagers = deadVillagers,
                RelationshipsOfAllLivingVillagers = relationshipsOfAllLivingVillagers
            }.Schedule(relationshipsOfAllLivingVillagers.Length, 1).Complete();

            relationshipsOfAllLivingVillagers.Dispose();

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

            new CleanupTargetFollowJob
            {
                DeadVillagers = deadVillagers
            }.ScheduleParallel();

            new CleanupIsMurderingJob
            {
                DeadVillagers = deadVillagers
            }.ScheduleParallel();

            // Destroy dead units
            state.EntityManager.DestroyEntity(_deadVillagerQuery);
            state.EntityManager.DestroyEntity(_deadBoarQuery);
            deadVillagers.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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