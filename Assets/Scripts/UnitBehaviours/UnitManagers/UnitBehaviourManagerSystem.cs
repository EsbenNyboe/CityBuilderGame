using Unity.Entities;

namespace UnitBehaviours.UnitManagers
{
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
            singleton.DecompositionDuration = config.DecompositionDuration;
            SystemAPI.SetSingleton(singleton);
        }
    }
}