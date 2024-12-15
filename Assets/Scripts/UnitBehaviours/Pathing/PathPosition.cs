using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.Pathing
{
    [InternalBufferCapacity(20)]
    public struct PathPosition : IBufferElementData
    {
        public int2 Position;
    }
}