using Unity.Entities;
using Unity.Mathematics;

namespace GridDebugging
{
    public struct DebugPopupEvent : IComponentData
    {
        public Entity Entity;
        public DebugPopupEventType Type;
        public int2 Cell;

        public bool IsInitialized;
        public float TimeWhenCreated;
    }

    public enum DebugPopupEventType
    {
        None,
        SleepOccupancyIssue,
        PathNotFoundStart,
        PathNotFoundEnd
    }
}