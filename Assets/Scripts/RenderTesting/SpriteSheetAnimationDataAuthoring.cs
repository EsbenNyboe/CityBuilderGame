using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationDataAuthoring : MonoBehaviour
{
    [SerializeField] private int _frameCount = 4;

    [SerializeField] private float _frameTimerMax = 0.1f;

    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationDataAuthoring>
    {
        public override void Bake(SpriteSheetAnimationDataAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,
                new SpriteSheetAnimationData
                {
                    FrameCount = authoring._frameCount,
                    FrameTimerMax = authoring._frameTimerMax
                });
        }
    }
}

public struct SpriteSheetAnimationData : IComponentData
{
    public int CurrentFrame;
    public int FrameCount;
    public float FrameTimer;
    public float FrameTimerMax;
    public Vector4 Uv;
    public Matrix4x4 Matrix;
}