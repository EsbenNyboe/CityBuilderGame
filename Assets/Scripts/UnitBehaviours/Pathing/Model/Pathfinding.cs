using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.Pathing
{
    public struct Pathfinding : IComponentData
    {
        public int2 StartPosition;
        public int2 EndPosition;
        public bool AllowNonWalkabes;
    }
}