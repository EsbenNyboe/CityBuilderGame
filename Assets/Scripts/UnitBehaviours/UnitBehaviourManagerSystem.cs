using Unity.Entities;

namespace UnitBehaviours
{
    public struct UnitBehaviourManager : IComponentData
    {
        public float DamagePerChop;
        public float MoveSpeedWhenAttemptingMurder;
    }

    public partial class UnitBehaviourManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<UnitBehaviourManager>();
        }

        protected override void OnUpdate()
        {
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            unitBehaviourManager.DamagePerChop = UnitBehaviourManagerConfig.Instance.DamagePerChop;
            unitBehaviourManager.MoveSpeedWhenAttemptingMurder =
                UnitBehaviourManagerConfig.Instance.MoveSpeedWhenAttemptingMurder;
            SystemAPI.SetSingleton(unitBehaviourManager);
        }
    }
}