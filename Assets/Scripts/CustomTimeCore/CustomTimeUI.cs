using UnityEngine;

namespace CustomTimeCore
{
    public class CustomTimeUI : MonoBehaviour
    {
        public static CustomTimeUI Instance;

        [Range(0.01f, 10)] public float TimeScale;

        private void Awake()
        {
            Instance = this;
        }
    }
}