using Effects.SocialEffectsRendering;
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
    public partial class SocialEventSystem : SystemBase
    {
        private EntityQuery _socialEventQuery;
        private EntityQuery _socialEventWithVictimQuery;

        protected override void OnCreate()
        {
            _socialEventQuery = GetEntityQuery(ComponentType.ReadOnly<SocialEvent>());
            _socialEventWithVictimQuery = GetEntityQuery(ComponentType.ReadOnly<SocialEventWithVictim>());
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            var isDebuggingBadPerformanceEvent = SystemAPI.GetSingleton<SocialDebugManager>().DebugBadPerformanceEvents;
            HandleSocialEvents(ecb, isDebuggingBadPerformanceEvent);
            HandleSocialEventsWithVictim(ecb, isDebuggingBadPerformanceEvent);

            EntityManager.DestroyEntity(_socialEventQuery);
            EntityManager.DestroyEntity(_socialEventWithVictimQuery);
        }

        private void HandleSocialEvents(EntityCommandBuffer ecb, bool isDebuggingBadPerformanceEvent)
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
                            if (isDebuggingBadPerformanceEvent)
                            {
                                PlayVisualEffect(influenceAmount, localTransform.ValueRO.Position);
                            }
                            else
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
        }

        private void HandleSocialEventsWithVictim(EntityCommandBuffer ecb, bool isDebuggingBadPerformanceEvent)
        {
            foreach (var socialEventWithVictimRefRO in SystemAPI.Query<RefRO<SocialEventWithVictim>>())
            {
                var socialEventWithVictim = socialEventWithVictimRefRO.ValueRO;
                foreach (var (socialRelationships, localTransform) in SystemAPI
                             .Query<RefRW<SocialRelationships>, RefRO<LocalTransform>>())
                {
                    var distance = Vector3.Distance(localTransform.ValueRO.Position, socialEventWithVictim.Position);
                    if (distance < socialEventWithVictim.InfluenceRadius)
                    {
                        var friendFactor = socialRelationships.ValueRO.Relationships[socialEventWithVictim.Victim];
                        var finalInfluenceAmount = socialEventWithVictim.InfluenceAmount * friendFactor;
                        socialRelationships.ValueRW.Relationships[socialEventWithVictim.Perpetrator] +=
                            finalInfluenceAmount;

                        if (finalInfluenceAmount != 0)
                        {
                            if (isDebuggingBadPerformanceEvent)
                            {
                                PlayVisualEffect(finalInfluenceAmount, localTransform.ValueRO.Position);
                            }
                            else
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

        private static void PlayVisualEffect(float influenceAmount, float3 eventPosition)
        {
            if (influenceAmount > 0)
            {
                SpriteEffectManager.Instance.PlaySocialPlusEffect(eventPosition);
            }
            else if (influenceAmount < 0)
            {
                SpriteEffectManager.Instance.PlaySocialMinusEffect(eventPosition);
            }
        }
    }
}