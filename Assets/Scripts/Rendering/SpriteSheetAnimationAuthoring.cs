using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpriteSheetAnimation());
        }
    }
}

public struct SpriteSheetAnimation : IComponentData
{
    public int CurrentFrame;
    public float FrameTimer;
    public Vector4 Uv;
    public Matrix4x4 Matrix;
}