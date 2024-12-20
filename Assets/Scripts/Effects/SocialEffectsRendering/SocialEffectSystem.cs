using Unity.Burst;
using Unity.Entities;

namespace Effects.SocialEffectsRendering
{
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
            
            InitializeNewSocialEffects(ref state, socialEffectSortingManager);
            UpdateSocialEffectStates(ref state, socialEffectSortingManager);
        }

        [BurstCompile]
        private void InitializeNewSocialEffects(ref SystemState state,
            SocialEffectSortingManager socialEffectSortingManager)
        {
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
        }

        [BurstCompile]
        private void UpdateSocialEffectStates(ref SystemState state, SocialEffectSortingManager socialEffectSortingManager)
        {
            foreach (var socialEffect in SystemAPI.Query<RefRW<SocialEffect>>().WithDisabled<SocialEffect>())
            {
                socialEffect.ValueRW.Position.y += socialEffectSortingManager.MoveSpeed * SystemAPI.Time.DeltaTime;
            }
        }
    }
}