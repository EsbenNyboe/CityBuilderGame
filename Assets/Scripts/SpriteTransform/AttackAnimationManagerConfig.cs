using UnityEngine;

namespace SpriteTransformNS
{
    public class AttackAnimationManagerConfig : MonoBehaviour
    {
        public static AttackAnimationManagerConfig Instance;

        public float AnimationDuration = 1f;

        public float AnimationSize = 0.5f;

        [Range(0f, 0.999f)] public float AnimationIdleTime = 0.1f;

        private void Awake()
        {
            Instance = this;
        }
    }
}