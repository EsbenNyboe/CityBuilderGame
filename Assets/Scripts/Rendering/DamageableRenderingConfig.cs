using UnityEngine;

namespace Rendering
{
    public class DamageableRenderingConfig : MonoBehaviour
    {
        public static DamageableRenderingConfig Instance;
        public Material Material;
        public Mesh Mesh;

        private void Awake()
        {
            Instance = this;
        }
    }
}