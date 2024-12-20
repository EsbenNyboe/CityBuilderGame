using Unity.Entities;

namespace Rendering
{
    public struct WorldSpriteSheetAnimation : IComponentData
    {
        public int CurrentFrame;
        public float FrameTimer;
    }
}