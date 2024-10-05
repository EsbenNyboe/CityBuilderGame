using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationDataAuthoring : MonoBehaviour
{
    [SerializeField] private int _currentFrame;

    [SerializeField] private int _frameCount = 4;

    [SerializeField] private float _frameTimer;

    [SerializeField] private float _frameTimerMax = 0.1f;

    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationDataAuthoring>
    {
        public override void Bake(SpriteSheetAnimationDataAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,
                new SpriteSheetAnimationData
                {
                    CurrentFrame = authoring._currentFrame,
                    FrameCount = authoring._frameCount,
                    FrameTimer = authoring._frameTimer,
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
}