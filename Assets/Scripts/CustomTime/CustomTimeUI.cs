using UnityEngine;

namespace CustomTime
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