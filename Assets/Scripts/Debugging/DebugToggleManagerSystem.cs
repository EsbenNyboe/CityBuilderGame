using Unity.Entities;

namespace Debugging
{
    public partial class DebugToggleManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<DebugToggleManager>();
            EntityManager.CreateSingleton<DebugToggleManager>();
        }

        protected override void OnUpdate()
        {
            var debugToggleManager = SystemAPI.GetSingletonRW<DebugToggleManager>();
            var debugToggleManagerInterface = DebugToggleManagerInterface.Instance;

            debugToggleManager.ValueRW.DebugSectionSorting =
                debugToggleManagerInterface.DebugSectionSorting;

            debugToggleManager.ValueRW.DebugPathInvalidation =
                debugToggleManagerInterface.DebugPathInvalidation;

            debugToggleManager.ValueRW.DebugPathfinding =
                debugToggleManagerInterface.DebugPathfinding;

            debugToggleManager.ValueRW.DebugPathSearchEmptyCells =
                debugToggleManagerInterface.DebugPathSearchEmptyCells;

            debugToggleManager.ValueRW.DebugBedOccupation =
                debugToggleManagerInterface.DebugBedOccupation;

            debugToggleManager.ValueRW.DebugBedSeeking =
                debugToggleManagerInterface.DebugBedSeeking;

            debugToggleManager.ValueRW.DebugTreeSeeking =
                debugToggleManagerInterface.DebugTreeSeeking;

            debugToggleManager.ValueRW.DebugTargetFollow =
                debugToggleManagerInterface.DebugTargetFollow;
        }
    }

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
    }
}