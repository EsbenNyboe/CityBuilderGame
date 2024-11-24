using UnityEngine;

namespace Effects.SocialEffectsRendering
{
    public class SocialEffectRendererManager : MonoBehaviour
    {
        public static SocialEffectRendererManager Instance;
        public Mesh Mesh;
        public Material Material;

        private void Awake()
        {
            Instance = this;
        }
    }
}