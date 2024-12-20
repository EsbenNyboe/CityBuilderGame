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
}