using Unity.Entities;
using Unity.Mathematics;

namespace Effects
{
    public struct DeathEvent : IComponentData
    {
        public float3 Position;
        public UnitType TargetType;
    }

    public struct DamageEvent : IComponentData
    {
        public float3 Position;
        public UnitType TargetType;
    }

    public enum UnitType
    {
        Villager,
        Boar,
        BabyVillager
    }
}