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
    /// For this type of event there is a perpetrator, a victim (in the positive or negative sense :))
    /// and some amount of observers (including perp and victim).
    /// Based on how much a given observer (dis)liked the *victim*, their opinion of the *perpetrator*
    /// will change. This means if you did something bad (e.g. murdered) to someone I like, I will
    /// dislike you more, but if you did it to someone I hate, I will like you more.
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
            _socialEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<SocialEvent>());
            _socialEventWithVictimQuery = state.GetEntityQuery(ComponentType.ReadOnly<SocialEvent>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            HandleSocialEvents(ref state);
            HandleSocialEventsWithVictim(ref state);

            state.EntityManager.DestroyEntity(_socialEventQuery);
            state.EntityManager.DestroyEntity(_socialEventWithVictimQuery);
        }

        private void HandleSocialEvents(ref SystemState state)
        {
            foreach (var socialEventRefRO in SystemAPI.Query<RefRO<SocialEvent>>())
            {
                var socialEvent = socialEventRefRO.ValueRO;
                foreach (var (socialRelationships, localTransform) in SystemAPI
                             .Query<RefRW<SocialRelationships>, RefRO<LocalTransform>>())
                {
                    var distance = Vector3.Distance(localTransform.ValueRO.Position, socialEvent.Position);
                    if (distance < socialEvent.InfluenceRadius)
                    {
                        socialRelationships.ValueRW.Relationships[socialEvent.Perpetrator] +=
                            socialEvent.InfluenceAmount;
                    }
                }
            }
        }

        private void HandleSocialEventsWithVictim(ref SystemState state)
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
                        float friendFactor = socialRelationships.ValueRO.Relationships[socialEventWithVictim.Victim];
                        float finalInfluenceAmount = socialEventWithVictim.InfluenceAmount * friendFactor;
                        socialRelationships.ValueRW.Relationships[socialEventWithVictim.Perpetrator] +=
                            finalInfluenceAmount;
                    }
                }
            }
        }
    }
}
