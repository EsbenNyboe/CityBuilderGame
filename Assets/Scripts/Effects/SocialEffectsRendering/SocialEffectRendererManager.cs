using UnityEngine;

namespace Effects.SocialEffectsRendering
{
    public class SocialEffectRendererManager : MonoBehaviour
    {
        public static SocialEffectRendererManager Instance;
        public Mesh Mesh;
        public Material Material;

        [Min(0)] public float Scale;
        [Min(0)] public float Offset;
        [Min(0)] public float Lifetime;
        [Min(0)] public float MoveSpeed;

        private void Awake()
        {
            Instance = this;
        }
    }
}