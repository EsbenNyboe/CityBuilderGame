using Debugging;
using Statistics;
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
        private EntityQuery _newSocialEffectsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<SocialEffectSortingManager>();

            _newSocialEffectsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(SocialEffect) }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var isCounting = SystemAPI.GetSingleton<DebugToggleManager>().CountSocialEffects;
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();

            var numOfPositiveEffects = 0;
            var numOfNegativeEffects = 0;

            // INITIALIZE NEW SOCIAL EFFECTS
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

                if (!isCounting)
                {
                    continue;
                }

                if (socialEffect.ValueRO.Type == SocialEffectType.Positive)
                {
                    numOfPositiveEffects++;
                }
                else
                {
                    numOfNegativeEffects++;
                }
            }

            if (isCounting)
            {
                UnitStatsDisplayManager.Instance.SetNumberOfPositiveSocialEffects(numOfPositiveEffects);
                UnitStatsDisplayManager.Instance.SetNumberOfNegativeSocialEffects(numOfNegativeEffects);
            }

            // UPDATE SOCIAL EFFECT STATES
            foreach (var socialEffect in SystemAPI.Query<RefRW<SocialEffect>>().WithDisabled<SocialEffect>())
            {
                socialEffect.ValueRW.Position.y += socialEffectSortingManager.MoveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}