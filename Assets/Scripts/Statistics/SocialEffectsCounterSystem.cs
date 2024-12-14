using Effects.SocialEffectsRendering;
using Unity.Entities;

namespace Statistics
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SocialEffectSystem))]
    public partial class SocialEffectsCounterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var numOfPositiveEffects = 0;
            var numOfNegativeEffects = 0;

            foreach (var (socialEffect, entity) in SystemAPI.Query<RefRO<SocialEffect>>().WithAll<SocialEffect>()
                         .WithEntityAccess())
            {
                if (socialEffect.ValueRO.Type == SocialEffectType.Positive)
                {
                    numOfPositiveEffects++;
                }
                else
                {
                    numOfNegativeEffects++;
                }
            }

            UnitStatsDisplayManager.Instance.SetNumberOfPositiveSocialEffects(numOfPositiveEffects);
            UnitStatsDisplayManager.Instance.SetNumberOfNegativeSocialEffects(numOfNegativeEffects);
        }
    }
}