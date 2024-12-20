using Unity.Entities;

namespace Debugging
{
    public struct DebugToggleManager : IComponentData
    {
        public bool DebugSectionSorting;
        public bool DebugPathInvalidation;
        public bool DebugPathfinding;
        public bool DebugPathSearchEmptyCells;
        public bool DebugBedOccupation;
        public bool DebugBedSeeking;
        public bool DebugTreeSeeking;
        public bool DebugTargetFollow;
        public bool DebugQuadrantSystem;
        public bool CountPathInvalidation;
        public bool CountSocialEffects;
    }
}