using Unity.Entities;
using UnityEngine;

public class AnimationUnitIdleAuthoring : MonoBehaviour
{
    [SerializeField] private int _frameCount;
    [SerializeField] private float _frameTimerMax;

    public class AnimationUnitIdleBaker : Baker<AnimationUnitIdleAuthoring>
    {
        public override void Bake(AnimationUnitIdleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AnimationUnitIdle { FrameCount = authoring._frameCount, FrameTimerMax = authoring._frameTimerMax });
            SetComponentEnabled<AnimationUnitIdle>(entity, false);
        }
    }
}

public struct AnimationUnitIdle : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public float FrameTimerMax;
}