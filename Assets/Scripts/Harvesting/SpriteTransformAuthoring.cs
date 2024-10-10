using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpriteTransformAuthoring : MonoBehaviour
{
    public class SpriteTransformBaker : Baker<SpriteTransformAuthoring>
    {
        public override void Bake(SpriteTransformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpriteTransform { Position = new float3(), Rotation = quaternion.identity });
        }
    }
}

public struct SpriteTransform : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
}