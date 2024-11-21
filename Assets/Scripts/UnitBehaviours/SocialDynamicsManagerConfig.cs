using UnityEngine;

namespace UnitBehaviours
{
    public class SocialDynamicsManagerConfig : MonoBehaviour
    {
        public static SocialDynamicsManagerConfig Instance;

        [Header("Social thresholds")] public float ThresholdForBecomingAnnoying = -1f;

        [Header("Social actions")] public float ImpactOnBedBeingOccupied = -1f;

        [Header("Social events")]
        public SocialEventConfig OnUnitAttackTree = new() { InfluenceAmount = 1f, InfluenceRadius = 10f };

        private void Awake()
        {
            Instance = this;
        }
    }
}