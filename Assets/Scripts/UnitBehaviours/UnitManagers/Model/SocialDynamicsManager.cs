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
}