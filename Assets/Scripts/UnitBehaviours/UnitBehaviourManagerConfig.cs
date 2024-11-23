using UnityEngine;

namespace UnitBehaviours
{
    public class UnitBehaviourManagerConfig : MonoBehaviour
    {
        public static UnitBehaviourManagerConfig Instance;
        public float DamagePerChop = 10f;
        public float DamagePerAttack = 10f;
        public float MoveSpeed = 5f;
        public float MoveSpeedWhenAttemptingMurder = 0.5f;

        private void Awake()
        {
            Instance = this;
        }
    }
}