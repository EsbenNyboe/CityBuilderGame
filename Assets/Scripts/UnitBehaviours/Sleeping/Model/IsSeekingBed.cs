using Unity.Entities;

namespace UnitBehaviours.Sleeping
{
    public struct IsSeekingBed : IComponentData
    {
        public int Attempts;
    }
}