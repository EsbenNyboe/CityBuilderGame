using Rendering;
using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting.Model
{
    public struct Renderable : IComponentData
    {
        public WorldSpriteSheetEntryType EntryType;
    }
}