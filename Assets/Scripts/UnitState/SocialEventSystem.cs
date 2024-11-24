using Effects.SocialEffectsRendering;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitState
{
    public struct SocialEvent : IComponentData
    {
        public Entity Perpetrator;
        public float3 Position;
        public float InfluenceAmount;
        public float InfluenceRadius;
    }

    /// <summary>
    ///     For this type of event there is a perpetrator, a victim (in the positive or negative sense :))
    ///     and some amount of observers (including perp and victim).
    ///     Based on how much a given observer (dis)liked the *victim*, their opinion of the *perpetrator*
    ///     will change. This means if you did something bad (e.g. murdered) to someone I like, I will
    ///     dislike you more, but if you did it to someone I hate, I will like you more.
    /// </summary>
    public struct SocialEventWithVictim : IComponentData
    {
        public Entity Perpetrator;
        public Entity Victim;
        public float3 Position;
        public float InfluenceAmount;
        public float InfluenceRadius;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(IsAliveSystem))]
    public partial struct SocialEventSystem : ISystem
    {
        private EntityQuery _socialEventQuery;
        private EntityQuery _socialEventWithVictimQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            _socialEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<SocialEvent>());
            _socialEventWithVictimQuery = state.GetEntityQuery(ComponentType.ReadOnly<SocialEventWithVictim>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            HandleSocialEvents(ref state, ecb);
            HandleSocialEventsWithVictim(ref state, ecb);

            state.EntityManager.DestroyEntity(_socialEventQuery);
            state.EntityManager.DestroyEntity(_socialEventWithVictimQuery);
        }

        private void HandleSocialEvents(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var socialEventRefRO in SystemAPI.Query<RefRO<SocialEvent>>())
            {
                var socialEvent = socialEventRefRO.ValueRO;
                foreach (var (socialRelationships, localTransform) in SystemAPI
                             .Query<RefRW<SocialRelationships>, RefRO<LocalTransform>>())
                {
                    var eventPosition = socialEvent.Position;
                    var distance = Vector3.Distance(localTransform.ValueRO.Position, eventPosition);
                    if (distance < socialEvent.InfluenceRadius)
                    {
                        var influenceAmount = socialEvent.InfluenceAmount;
                        socialRelationships.ValueRW.Relationships[socialEvent.Perpetrator] +=
                            influenceAmount;

                        if (influenceAmount != 0)
                        {
                            ecb.AddComponent(ecb.CreateEntity(), new SocialEffect
                            {
                                Position = localTransform.ValueRO.Position,
                                Type = influenceAmount > 0
                                    ? SocialEffectType.Positive
                                    : SocialEffectType.Negative
                            });
                        }
                    }
                }
            }
        }

        private void HandleSocialEventsWithVictim(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var socialEventWithVictimRefRO in SystemAPI.Query<RefRO<SocialEventWithVictim>>())
            {
                var socialEventWithVictim = socialEventWithVictimRefRO.ValueRO;
                foreach (var (socialRelationships, localTransform, entity) in SystemAPI
                             .Query<RefRW<SocialRelationships>, RefRO<LocalTransform>>().WithEntityAccess())
                {
                    var distance = Vector3.Distance(localTransform.ValueRO.Position, socialEventWithVictim.Position);
                    if (distance < socialEventWithVictim.InfluenceRadius)
                    {
                        var friendFactor = socialRelationships.ValueRO.Relationships[socialEventWithVictim.Victim];
                        var finalInfluenceAmount = socialEventWithVictim.InfluenceAmount * friendFactor;
                        if (entity == socialEventWithVictim.Victim)
                        {
                            // If it's happening to me, I take it more personal than others.
                            finalInfluenceAmount += socialEventWithVictim.InfluenceAmount;
                        }

                        socialRelationships.ValueRW.Relationships[socialEventWithVictim.Perpetrator] +=
                            finalInfluenceAmount;

                        if (finalInfluenceAmount != 0)
                        {
                            ecb.AddComponent(ecb.CreateEntity(), new SocialEffect
                            {
                                Position = localTransform.ValueRO.Position,
                                Type = finalInfluenceAmount > 0
                                    ? SocialEffectType.Positive
                                    : SocialEffectType.Negative
                            });
                        }
                    }
                }
            }
        }
    }
}