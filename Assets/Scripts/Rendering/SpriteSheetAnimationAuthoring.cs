using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    // [SerializeField] private int _frameCount = 4;
    //
    // [SerializeField] private float _frameTimerMax = 0.1f;

    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,
                new SpriteSheetAnimation
                {
                    // FrameCount = authoring._frameCount,
                    // FrameTimerMax = authoring._frameTimerMax
                });
        }
    }
}

public struct SpriteSheetAnimation : IComponentData
{
    public int CurrentFrame;
    public int FrameCount;
    public float FrameTimer;
    public float FrameTimerMax;
    public Vector4 Uv;
    public Matrix4x4 Matrix;
}