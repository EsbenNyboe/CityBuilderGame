using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    [SerializeField] private AnimationConfig _idleAnimationConfig;
    [SerializeField] private AnimationConfig _walkAnimationConfig;
    [SerializeField] private AnimationConfig _sleepAnimationConfig;

    [Serializable]
    public class AnimationConfig
    {
        public int FrameCount;
        public int FrameRow;
        public int SpriteColumns;
        public int SpriteRows;

        [FormerlySerializedAs("FrameTimerMax")]
        public float FrameInterval;
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
                SpriteColumns = authoring._idleAnimationConfig.SpriteColumns,
                SpriteRows = authoring._idleAnimationConfig.SpriteRows,
                FrameTimerMax = authoring._idleAnimationConfig.FrameInterval
            });
            SetComponentEnabled<AnimationUnitIdle>(entity, false);

            // Walk animation config
            AddComponent(entity, new AnimationUnitWalk
            {
                FrameCount = authoring._walkAnimationConfig.FrameCount,
                FrameRow = authoring._walkAnimationConfig.FrameRow,
                SpriteColumns = authoring._walkAnimationConfig.SpriteColumns,
                SpriteRows = authoring._walkAnimationConfig.SpriteRows,
                FrameTimerMax = authoring._walkAnimationConfig.FrameInterval
            });
            SetComponentEnabled<AnimationUnitWalk>(entity, false);

            // Walk animation config
            AddComponent(entity, new AnimationUnitSleep
            {
                FrameCount = authoring._sleepAnimationConfig.FrameCount,
                FrameRow = authoring._sleepAnimationConfig.FrameRow,
                SpriteColumns = authoring._sleepAnimationConfig.SpriteColumns,
                SpriteRows = authoring._sleepAnimationConfig.SpriteRows,
                FrameTimerMax = authoring._sleepAnimationConfig.FrameInterval
            });
            SetComponentEnabled<AnimationUnitSleep>(entity, false);

            // Animation component: Set idle animation as default
            AddComponent(entity, new SpriteSheetAnimation
            {
                FrameCount = authoring._idleAnimationConfig.FrameCount,
                FrameTimerMax = authoring._idleAnimationConfig.FrameInterval,
                Uv = new Vector4(1f / authoring._idleAnimationConfig.FrameCount, 1 / 3f, 0, authoring._idleAnimationConfig.FrameRow)
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
    public int FrameRow;
    public int SpriteColumns;
    public int SpriteRows;
    public float FrameTimerMax;
}

public struct AnimationUnitWalk : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public int FrameRow;
    public int SpriteColumns;
    public int SpriteRows;
    public float FrameTimerMax;
}

public struct AnimationUnitSleep : IComponentData, IEnableableComponent
{
    public int FrameCount;
    public int FrameRow;
    public int SpriteColumns;
    public int SpriteRows;
    public float FrameTimerMax;
}