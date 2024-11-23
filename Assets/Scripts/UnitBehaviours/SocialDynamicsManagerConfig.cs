using UnityEngine;

namespace UnitBehaviours
{
    public class SocialDynamicsManagerConfig : MonoBehaviour
    {
        public static SocialDynamicsManagerConfig Instance;

        [Range(0.000000001f, 0.1f)] public float NeutralizationFactor = 0.01f;

        public float ThresholdForBecomingAnnoying = -1f;

        public float ImpactOnBedBeingOccupied = -1f;

        public SocialEventConfig OnUnitAttackTree = new() { InfluenceAmount = 1f, InfluenceRadius = 10f };
        public SocialEventConfig OnUnitAttackUnit = new() { InfluenceAmount = -1f, InfluenceRadius = 10f };

        private void Awake()
        {
            Instance = this;
        }
    }
}