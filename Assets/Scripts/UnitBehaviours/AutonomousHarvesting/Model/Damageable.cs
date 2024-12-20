using Unity.Entities;

namespace GridEntityNS
{
    public struct Damageable : IComponentData
    {
        public float HealthNormalized;
    }
}