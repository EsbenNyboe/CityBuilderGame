using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Idle animation config
            AddComponent(entity, new AnimationUnitIdle());
            SetComponentEnabled<AnimationUnitIdle>(entity, false);

            // Walk animation config
            AddComponent(entity, new AnimationUnitWalk());
            SetComponentEnabled<AnimationUnitWalk>(entity, false);

            // Walk animation config
            AddComponent(entity, new AnimationUnitSleep());
            SetComponentEnabled<AnimationUnitSleep>(entity, false);

            // Animation component: Set idle animation as default
            AddComponent(entity, new SpriteSheetAnimation());
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

public struct AnimationUnitIdle : IComponentData, IEnableableComponent
{
}

public struct AnimationUnitWalk : IComponentData, IEnableableComponent
{
}

public struct AnimationUnitSleep : IComponentData, IEnableableComponent
{
}