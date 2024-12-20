using Unity.Entities;
using Unity.Mathematics;

namespace UnitBehaviours.ActionGateNS
{
    public struct RandomContainer : IComponentData, IEnableableComponent
    {
        public Random Random;
    }
}