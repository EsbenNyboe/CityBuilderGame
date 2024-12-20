using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.Targeting
{
    public struct Spear : IComponentData
    {
        public float2 Direction;
        public float2 CurrentPosition;
        public int2 Target;
    }
}