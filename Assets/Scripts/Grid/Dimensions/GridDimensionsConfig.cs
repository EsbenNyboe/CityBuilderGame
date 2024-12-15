using UnityEngine;

namespace Grid
{
    public class GridDimensionsConfig : MonoBehaviour
    {
        public static GridDimensionsConfig Instance;
        [Min(1)] public int Width;
        [Min(1)] public int Height;

        private void Awake()
        {
            Instance = this;
        }
    }
}