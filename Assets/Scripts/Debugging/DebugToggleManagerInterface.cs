using UnityEngine;

namespace Debugging
{
    public class DebugToggleManagerInterface : MonoBehaviour
    {
        public static DebugToggleManagerInterface Instance;
        public bool DebugSectionSorting;
        public bool DebugPathfinding;
        public bool DebugPathSearchEmptyCells;
        public bool DebugBedSeeking;
        public bool DebugTreeSeeking;
        public bool DebugPathInvalidation;

        private void Awake()
        {
            Instance = this;
        }
    }
}