using Unity.Entities;

namespace Rendering
{
    public struct UnitAnimationSelection : IComponentData
    {
        public WorldSpriteSheetEntryType SelectedAnimation;
        public WorldSpriteSheetEntryType CurrentAnimation;
    }
}