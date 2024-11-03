using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsHarvesting : IComponentData
    {
        public readonly int2 Tree;
        public float TimeUntilNextChop;

        public IsHarvesting(int2 tree)
        {
            Tree = tree;
            TimeUntilNextChop = 0f;
        }
    }
}