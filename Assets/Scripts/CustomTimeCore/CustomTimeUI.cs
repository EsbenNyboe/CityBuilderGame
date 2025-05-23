using UnityEngine;

namespace CustomTimeCore
{
    public class CustomTimeUI : MonoBehaviour
    {
        public static CustomTimeUI Instance;
        public float TimeScale;

        private void Awake()
        {
            Instance = this;
        }
    }
}