using Unity.Entities;

namespace UnitState.Dead
{
    public struct Corpse : IComponentData
    {
        public int MeatCurrent;
        public int MeatMax;
        public float CurrentDecomposition; // TODO: Add Decomposition logic
    }
}