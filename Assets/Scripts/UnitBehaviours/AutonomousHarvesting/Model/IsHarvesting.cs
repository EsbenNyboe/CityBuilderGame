using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsHarvesting : IComponentData
    {
        public HarvestableType HarvestableType; // TODO: Replace with gridManager-stuff instead

        public IsHarvesting(HarvestableType harvestableType)
        {
            HarvestableType = harvestableType;
        }
    }

    public enum HarvestableType
    {
        Tree,
        BerryBush
    }
}