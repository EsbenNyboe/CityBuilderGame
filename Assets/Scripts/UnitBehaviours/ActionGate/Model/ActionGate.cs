using Unity.Entities;

namespace UnitBehaviours.ActionGateNS
{
    public struct ActionGate : IComponentData
    {
        // TODO: Do not apply TimeScale to this variable:
        public float MinTimeOfAction;
    }
}