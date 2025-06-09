using Unity.Entities;

namespace UnitBehaviours.Hunger
{
    public struct IsCookingMeat : IComponentData
    {
        public float CookingProgress;
    }
}