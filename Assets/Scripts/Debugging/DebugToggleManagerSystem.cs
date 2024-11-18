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

            debugToggleManager.ValueRW.DebugPathfinding =
                debugToggleManagerInterface.DebugPathfinding;

            debugToggleManager.ValueRW.DebugPathSearchEmptyCells =
                debugToggleManagerInterface.DebugPathSearchEmptyCells;
        }
    }

    public struct DebugToggleManager : IComponentData
    {
        public bool DebugSectionSorting;
        public bool DebugPathfinding;
        public bool DebugPathSearchEmptyCells;
    }
}