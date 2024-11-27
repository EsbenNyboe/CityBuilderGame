using Events;
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
        private EntityQuery _deadUnits;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<SocialEvaluationManager>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<IsAlive>();

            _deadUnits = new EntityQueryBuilder(Allocator.Temp).WithDisabled<IsAlive>().Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var gridManagerRW = SystemAPI.GetSingletonRW<GridManager>();
            using var deadUnits = _deadUnits.ToEntityArray(Allocator.Temp);
            using var invalidSocialEvents = new NativeList<Entity>(Allocator.Temp);
            using var invalidSocialEventsWithVictim = new NativeList<Entity>(Allocator.Temp);
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Cleanup social events
            foreach (var (socialEvent, entity) in SystemAPI.Query<RefRO<SocialEvent>>().WithEntityAccess())
            {
                for (var i = 0; i < deadUnits.Length; i++)
                {
                    if (socialEvent.ValueRO.Perpetrator == deadUnits[i])
                    {
                        invalidSocialEvents.Add(entity);
                    }
                }
            }

            // Cleanup social events with victim
            foreach (var (socialEventWithVictim, entity) in SystemAPI.Query<RefRO<SocialEventWithVictim>>()
                         .WithEntityAccess())
            {
                for (var i = 0; i < deadUnits.Length; i++)
                {
                    if (socialEventWithVictim.ValueRO.Perpetrator == deadUnits[i] ||
                        socialEventWithVictim.ValueRO.Victim == deadUnits[i])
                    {
                        invalidSocialEventsWithVictim.Add(entity);
                    }
                }
            }

            // Play death effect
            foreach (var localTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>())
            {
                ecb.AddComponent(ecb.CreateEntity(),
                    new DeathEvent { Position = localTransform.ValueRO.Position });
            }

            // Cleanup grid
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>()
                         .WithEntityAccess())
            {
                var position = localTransform.ValueRO.Position;
                gridManagerRW.ValueRW.OnUnitDestroyed(entity, position);
            }

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
                foreach (var deadUnit in deadUnits)
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
                foreach (var deadUnit in deadUnits)
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
                foreach (var deadUnit in deadUnits)
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
                foreach (var deadUnit in deadUnits)
                {
                    if (deadUnit == isMurdering.ValueRO.Target)
                    {
                        isMurdering.ValueRW.Target = Entity.Null;
                    }
                }
            }

            // Destroy dead units
            state.EntityManager.DestroyEntity(deadUnits);
            state.EntityManager.DestroyEntity(invalidSocialEvents.AsArray());
            state.EntityManager.DestroyEntity(invalidSocialEventsWithVictim.AsArray());
        }
    }
}