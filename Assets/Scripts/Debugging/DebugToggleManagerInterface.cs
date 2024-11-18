using UnityEngine;

namespace Debugging
{
    public class DebugToggleManagerInterface : MonoBehaviour
    {
        public static DebugToggleManagerInterface Instance;
        public bool IsDebugging;
        public bool DebugPathfinding;

        private void Awake()
        {
            Instance = this;
        }
    }
}