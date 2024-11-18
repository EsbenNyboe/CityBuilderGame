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

            debugToggleManager.ValueRW.IsDebugging = debugToggleManagerInterface.IsDebugging;
            debugToggleManager.ValueRW.DebugPathfinding = debugToggleManagerInterface.DebugPathfinding;
        }
    }

    public struct DebugToggleManager : IComponentData
    {
        public bool IsDebugging;
        public bool DebugPathfinding;
    }
}