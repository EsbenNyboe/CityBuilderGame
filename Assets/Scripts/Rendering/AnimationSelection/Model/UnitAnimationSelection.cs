using Unity.Entities;

namespace Rendering
{
    public struct UnitAnimationSelection : IComponentData
    {
        public WorldSpriteSheetEntryType SelectedAnimation;
        public WorldSpriteSheetEntryType CurrentAnimation;

        public readonly bool IsSitting()
        {
            return CurrentAnimation is WorldSpriteSheetEntryType.VillagerEat or WorldSpriteSheetEntryType.BabyEat;
        }
    }
}