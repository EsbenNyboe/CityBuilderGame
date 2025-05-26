using CustomTimeCore;
using Unity.Burst;
using Unity.Entities;

namespace Effects.SocialEffectsRendering
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SocialEffectSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<SocialEffectSortingManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();

            InitializeNewSocialEffects(ref state, timeScale, socialEffectSortingManager);
            UpdateSocialEffectStates(ref state, timeScale, socialEffectSortingManager);
        }

        [BurstCompile]
        private void InitializeNewSocialEffects(ref SystemState state, float timeScale,
            SocialEffectSortingManager socialEffectSortingManager)
        {
            foreach (var (socialEffect, entity) in SystemAPI.Query<RefRO<SocialEffect>>().WithAll<SocialEffect>()
                         .WithEntityAccess())
            {
                SystemAPI.SetComponentEnabled<SocialEffect>(entity, false);
                socialEffectSortingManager.SocialEffectQueue.Enqueue(new SocialEffectData
                {
                    Entity = entity,
                    TimeCreated = (float)SystemAPI.Time.ElapsedTime * timeScale,
                    Type = socialEffect.ValueRO.Type
                });
            }
        }

        [BurstCompile]
        private void UpdateSocialEffectStates( ref SystemState state, float timeScale,
            SocialEffectSortingManager socialEffectSortingManager)
        {
            foreach (var socialEffect in SystemAPI.Query<RefRW<SocialEffect>>().WithDisabled<SocialEffect>())
            {
                socialEffect.ValueRW.Position.y += socialEffectSortingManager.MoveSpeed * SystemAPI.Time.DeltaTime * timeScale;
            }
        }
    }
}