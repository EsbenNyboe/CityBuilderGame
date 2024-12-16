using System;
using Unity.Entities;

namespace UnitBehaviours.UnitManagers
{
    public struct SocialDynamicsManager : IComponentData
    {
        public float NeutralizationFactor;
        public float ThresholdForBecomingAnnoying;
        public float ImpactOnBedBeingOccupied;
        public SocialEventConfig OnUnitAttackTree;
        public SocialEventConfig OnUnitAttackUnit;
    }

    [Serializable]
    public struct SocialEventConfig
    {
        public float InfluenceAmount;
        public float InfluenceRadius;
    }

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