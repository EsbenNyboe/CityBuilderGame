using Events;
using UnitBehaviours;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitState
{
    public struct IsAlive : IComponentData, IEnableableComponent
    {
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial struct IsAliveSystem : ISystem
    {
        private EntityQuery _deadVillagers;
        private EntityQuery _deadBoars;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<SocialEvaluationManager>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<IsAlive>();

            _deadBoars = state.GetEntityQuery(new EntityQueryDesc
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
            _deadVillagers = state.GetEntityQuery(new EntityQueryDesc
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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            using var deadVillagers = _deadVillagers.ToEntityArray(Allocator.Temp);
            if (deadVillagers.Length <= 0)
            {
                return;
            }

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            using var invalidSocialEvents = new NativeList<Entity>(Allocator.Temp);
            using var invalidSocialEventsWithVictim = new NativeList<Entity>(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

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

            // Play death effect
            new PlayDeathEffectJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                UnitType = UnitType.Villager
            }.ScheduleParallel(_deadVillagers, state.Dependency).Complete();

            new PlayDeathEffectJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                UnitType = UnitType.Boar
            }.ScheduleParallel(_deadBoars, state.Dependency).Complete();

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

            // Cleanup alive units relationships
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithAll<IsAlive>())
            {
                foreach (var deadUnit in deadVillagers)
                {
                    socialRelationships.ValueRW.Relationships.Remove(deadUnit);
                }
            }

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

            // Cleanup TargetFollow targets
            foreach (var targetFollow in SystemAPI.Query<RefRW<TargetFollow>>())
            {
                foreach (var deadUnit in deadVillagers)
                {
                    if (deadUnit == targetFollow.ValueRO.Target)
                    {
                        targetFollow.ValueRW.Target = Entity.Null;
                        targetFollow.ValueRW.CurrentDistanceToTarget = math.INFINITY;
                        targetFollow.ValueRW.DesiredRange = 0;
                    }
                }
            }

            // Cleanup IsMurdering targets
            foreach (var isMurdering in SystemAPI.Query<RefRW<IsMurdering>>())
            {
                foreach (var deadUnit in deadVillagers)
                {
                    if (deadUnit == isMurdering.ValueRO.Target)
                    {
                        isMurdering.ValueRW.Target = Entity.Null;
                    }
                }
            }

            // Destroy dead units
            state.EntityManager.DestroyEntity(deadVillagers);
            state.EntityManager.DestroyEntity(invalidSocialEvents.AsArray());
            state.EntityManager.DestroyEntity(invalidSocialEventsWithVictim.AsArray());
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

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