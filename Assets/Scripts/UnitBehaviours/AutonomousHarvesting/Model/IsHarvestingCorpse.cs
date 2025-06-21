using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsHarvestingCorpse : IComponentData
    {
        public Entity Target;
    }
}