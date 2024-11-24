using Unity.Entities;
using Unity.Mathematics;

namespace Effects.SocialEffectsRendering
{
    public struct SocialEffect : IComponentData, IEnableableComponent
    {
        public float3 Position;
        public SocialEffectType Type;
    }

    public enum SocialEffectType
    {
        None,
        Positive,
        Negative
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SocialEffectSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SocialEffectSortingManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();

            foreach (var (socialEffect, entity) in SystemAPI.Query<RefRO<SocialEffect>>().WithAll<SocialEffect>()
                         .WithEntityAccess())
            {
                SystemAPI.SetComponentEnabled<SocialEffect>(entity, false);
                socialEffectSortingManager.SocialEffectQueue.Enqueue(new SocialEffectData
                {
                    Entity = entity,
                    TimeCreated = (float)SystemAPI.Time.ElapsedTime,
                    Type = socialEffect.ValueRO.Type
                });
            }

            foreach (var socialEffect in SystemAPI.Query<RefRW<SocialEffect>>().WithDisabled<SocialEffect>())
            {
                socialEffect.ValueRW.Position.y += socialEffectSortingManager.MoveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}