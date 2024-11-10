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

    public partial struct SocialEventSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (socialRelationships, localTransform) in SystemAPI
                         .Query<RefRW<SocialRelationships>, RefRO<LocalTransform>>())
            {
                foreach (var socialEventRefRO in SystemAPI.Query<RefRO<SocialEvent>>())
                {
                    var socialEvent = socialEventRefRO.ValueRO;
                    var distance = Vector3.Distance(localTransform.ValueRO.Position, socialEvent.Position);
                    if (distance < socialEvent.InfluenceRadius)
                    {
                        socialRelationships.ValueRW.Relationships[socialEvent.Perpetrator] +=
                            socialEvent.InfluenceAmount;
                    }
                }
            }

            foreach (var (socialEvent, entity) in SystemAPI.Query<RefRO<SocialEvent>>().WithEntityAccess())
            {
                ecb.RemoveComponent<SocialEvent>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}