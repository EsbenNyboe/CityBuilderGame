using Unity.Entities;

namespace UnitBehaviours.Pathing
{
    public partial struct PathFollow : IComponentData
    {
        public int PathIndex;
        public float MoveSpeedMultiplier;

        public PathFollow(int pathIndex, float moveSpeedMultiplier = 1)
        {
            PathIndex = pathIndex;
            MoveSpeedMultiplier = moveSpeedMultiplier;
        }
    }
    
    public partial struct PathFollow
    {
        public readonly bool IsMoving()
        {
            return PathIndex >= 0;
        }
    }
}