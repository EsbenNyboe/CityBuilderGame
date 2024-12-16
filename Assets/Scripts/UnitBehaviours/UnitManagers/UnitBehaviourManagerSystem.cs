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
    }

    public partial class UnitBehaviourManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<UnitBehaviourManager>();
        }

        protected override void OnUpdate()
        {
            var singleton = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var config = UnitBehaviourManagerConfig.Instance;
            singleton.DamagePerChop = config.DamagePerChop;
            singleton.DamagePerAttack = config.DamagePerAttack;
            singleton.MoveSpeed = config.MoveSpeed;
            singleton.MoveSpeedWhenAttemptingMurder = config.MoveSpeedWhenAttemptingMurder;
            singleton.MaxSeekAttempts = config.MaxSeekAttempts;
            singleton.BoarQuadrantRange = config.BoarQuadrantRange;
            SystemAPI.SetSingleton(singleton);
        }
    }
}