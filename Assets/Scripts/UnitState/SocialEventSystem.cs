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

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(IsAliveSystem))]
    public partial struct SocialEventSystem : ISystem
    {
        private EntityQuery _socialEventQuery;

        public void OnCreate(ref SystemState state)
        {
            _socialEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<SocialEvent>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
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
                        var relationships = socialRelationships.ValueRO.Relationships;
                        if (relationships.ContainsKey(socialEvent.Perpetrator))
                        {
                            socialRelationships.ValueRW.Relationships[socialEvent.Perpetrator] +=
                                socialEvent.InfluenceAmount;
                        }
                        else
                        {
                            Debug.LogError("Social Relationship Not Found");
                        }
                    }
                }
            }

            state.EntityManager.DestroyEntity(_socialEventQuery);
        }
    }
}