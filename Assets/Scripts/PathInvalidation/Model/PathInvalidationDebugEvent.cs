using Unity.Entities;

namespace PathInvalidation
{
    public struct PathInvalidationDebugEvent : IComponentData
    {
        public int Count;
    }
}