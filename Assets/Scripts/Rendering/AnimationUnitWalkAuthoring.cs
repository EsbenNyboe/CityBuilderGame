using Unity.Entities;
using UnityEngine;

public class AnimationUnitWalkAuthoring : MonoBehaviour
{
    [SerializeField] private int _frameCount;
    [SerializeField] private float _frameRow;
    [SerializeField] private float _frameTimerMax;

    public class AnimationUnitWalkBaker : Baker<AnimationUnitWalkAuthoring>
    {
        public override void Bake(AnimationUnitWalkAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AnimationUnitWalk
            {
                FrameCount = authoring._frameCount,
                FrameRow = authoring._frameRow,
                FrameTimerMax = authoring._frameTimerMax
            });
            SetComponentEnabled<AnimationUnitWalk>(entity, false);
        }
    }
}

public struct AnimationUnitWalk : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public float FrameRow;
    public float FrameTimerMax;
}