using Unity.Entities;

namespace UnitBehaviours.Targeting
{
    public struct Health : IComponentData
    {
        public float CurrentHealth;
        public float MaxHealth;
    }
}