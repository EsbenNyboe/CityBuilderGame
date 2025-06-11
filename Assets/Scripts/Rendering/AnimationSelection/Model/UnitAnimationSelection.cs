using Unity.Entities;

namespace Rendering
{
    public struct UnitAnimationSelection : IComponentData
    {
        public WorldSpriteSheetEntryType SelectedAnimation;
        public WorldSpriteSheetEntryType CurrentAnimation;

        public readonly bool IsEating()
        {
            return CurrentAnimation is WorldSpriteSheetEntryType.VillagerEat or WorldSpriteSheetEntryType.BabyEat;
        }

        public readonly bool IsBabyHoldingItem()
        {
            return CurrentAnimation is WorldSpriteSheetEntryType.BabyIdleHolding
                or WorldSpriteSheetEntryType.BabyWalkHolding
                or WorldSpriteSheetEntryType.BabyEat
                or WorldSpriteSheetEntryType.BabyWalk
                or WorldSpriteSheetEntryType.BabyIdle;
        }
    }
}