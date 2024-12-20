using Unity.Entities;
using Unity.Mathematics;

namespace SpriteTransformNS
{
    /// <summary>
    ///     When an attack-animation is marked for deletion,
    ///     it needs to finish the animation, before the unit can make a new decision.
    /// </summary>
    public struct AttackAnimation : IComponentData
    {
        public float2 Target;
        public float TimeLeft;
        public bool MarkedForDeletion;

        public AttackAnimation(int2 target, float timeLeft = 0)
        {
            Target = target;
            TimeLeft = timeLeft;
            MarkedForDeletion = false;
        }
    }
}