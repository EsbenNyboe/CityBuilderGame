using System;
using Unity.Entities;
using UnityEngine;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    [SerializeField] private AnimationConfig _idleAnimationConfig;
    [SerializeField] private AnimationConfig _walkAnimationConfig;

    [Serializable]
    public class AnimationConfig
    {
        public int FrameCount;
        public float FrameRow;
        public float FrameTimerMax;
    }

    public class SpriteSheetAnimationDataBaker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Idle animation config
            AddComponent(entity, new AnimationUnitIdle
            {
                FrameCount = authoring._idleAnimationConfig.FrameCount,
                FrameRow = authoring._idleAnimationConfig.FrameRow,
                FrameTimerMax = authoring._idleAnimationConfig.FrameTimerMax
            });
            SetComponentEnabled<AnimationUnitIdle>(entity, false);

            // Walk animation config
            AddComponent(entity, new AnimationUnitWalk
            {
                FrameCount = authoring._walkAnimationConfig.FrameCount,
                FrameRow = authoring._walkAnimationConfig.FrameRow,
                FrameTimerMax = authoring._walkAnimationConfig.FrameTimerMax
            });
            SetComponentEnabled<AnimationUnitWalk>(entity, false);

            // Animation component: Set idle animation as default
            AddComponent(entity, new SpriteSheetAnimation
            {
                FrameCount = authoring._idleAnimationConfig.FrameCount,
                FrameTimerMax = authoring._idleAnimationConfig.FrameTimerMax,
                Uv = new Vector4(1f / authoring._idleAnimationConfig.FrameCount, 0.5f, 0, authoring._idleAnimationConfig.FrameRow)
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

public struct AnimationUnitIdle : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public float FrameRow;
    public float FrameTimerMax;
}

public struct AnimationUnitWalk : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public float FrameRow;
    public float FrameTimerMax;
}