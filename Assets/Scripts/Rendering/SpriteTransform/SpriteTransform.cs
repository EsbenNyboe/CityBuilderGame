using Unity.Entities;
using Unity.Mathematics;

namespace Rendering.SpriteTransformNS
{
    public struct SpriteTransform : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
    }
}