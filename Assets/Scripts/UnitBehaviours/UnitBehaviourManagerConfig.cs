using UnityEngine;

namespace UnitBehaviours
{
    public class UnitBehaviourManagerConfig : MonoBehaviour
    {
        public static UnitBehaviourManagerConfig Instance;
        public float DamagePerChop = 10f;

        private void Awake()
        {
            Instance = this;
        }
    }
}