using Unity.Entities;

namespace UnitBehaviours.UnitManagers
{
    public struct UnitBehaviourManager : IComponentData
    {
        public float DamagePerChop;
        public float DamagePerAttack;
        public float MoveSpeed;
        public float MoveSpeedWhenAttemptingMurder;
        public int MaxSeekAttempts;
        public int BoarQuadrantRange;
        public float DecompositionDuration;
        public int QuadrantSearchRange;
    }
}