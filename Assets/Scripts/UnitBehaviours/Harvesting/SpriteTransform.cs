using Unity.Entities;
using Unity.Mathematics;

public struct SpriteTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
}