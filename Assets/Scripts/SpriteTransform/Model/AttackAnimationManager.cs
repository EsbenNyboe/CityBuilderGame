using Unity.Entities;

namespace SpriteTransformNS
{
    public struct AttackAnimationManager : IComponentData
    {
        public float AttackDuration;
        public float AttackAnimationSize;
        public float AttackAnimationIdleTime;
    }
}