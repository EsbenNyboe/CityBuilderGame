using Unity.Entities;

namespace UnitBehaviours.UnitManagers
{
    public partial class SocialDynamicsManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<SocialDynamicsManager>();
        }

        protected override void OnUpdate()
        {
            var singleton = SystemAPI.GetSingleton<SocialDynamicsManager>();
            var config = SocialDynamicsManagerConfig.Instance;
            singleton.NeutralizationFactor = config.NeutralizationFactor;
            singleton.ThresholdForBecomingAnnoying = config.ThresholdForBecomingAnnoying;
            singleton.ImpactOnBedBeingOccupied = config.ImpactOnBedBeingOccupied;
            singleton.OnUnitAttackTree = config.OnUnitAttackTree;
            singleton.OnUnitAttackUnit = config.OnUnitAttackUnit;
            SystemAPI.SetSingleton(singleton);
        }
    }
}