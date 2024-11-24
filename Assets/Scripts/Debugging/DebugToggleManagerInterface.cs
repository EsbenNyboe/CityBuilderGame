using UnityEngine;

namespace Debugging
{
    public class DebugToggleManagerInterface : MonoBehaviour
    {
        public static DebugToggleManagerInterface Instance;
        public bool DebugSectionSorting;
        public bool DebugPathfinding;
        public bool DebugPathSearchEmptyCells;
        public bool DebugBedOccupation;
        public bool DebugBedSeeking;
        public bool DebugTreeSeeking;
        public bool DebugPathInvalidation;
        public bool DebugTargetFollow;
        public bool DebugQuadrantSystem;

        private void Awake()
        {
            Instance = this;
        }
    }
}