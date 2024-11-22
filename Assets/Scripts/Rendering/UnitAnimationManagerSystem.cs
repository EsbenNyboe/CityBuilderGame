using System;
using Unity.Entities;

public struct UnitAnimationManager : IComponentData
{
    public AnimationConfig TalkAnimation;
    public AnimationConfig SleepAnimation;
    public AnimationConfig WalkAnimation;
    public AnimationConfig IdleAnimation;
    public int SpriteColumns;
    public int SpriteRows;
}

[Serializable]
public struct AnimationConfig
{
    public AnimationId Identifier;
    public int SpriteRow;
    public int FrameCount;
    public float FrameInterval;
}

public enum AnimationId
{
    None,
    Talk,
    Sleep,
    Walk,
    Idle
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
public partial class UnitAnimationManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        var unitAnimationManager = new UnitAnimationManager
        {
            TalkAnimation = new AnimationConfig
            {
                SpriteRow = 0,
                FrameCount = 2,
                FrameInterval = 0.4f
            },
            SleepAnimation = new AnimationConfig
            {
                SpriteRow = 1,
                FrameCount = 3,
                FrameInterval = 0.4f
            },
            WalkAnimation = new AnimationConfig
            {
                SpriteRow = 2,
                FrameCount = 2,
                FrameInterval = 0.11f
            },
            IdleAnimation = new AnimationConfig
            {
                SpriteRow = 3,
                FrameCount = 2,
                FrameInterval = 0.8f
            },
            SpriteColumns = 3,
            SpriteRows = 4
        };
        EntityManager.AddComponent<UnitAnimationManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, unitAnimationManager);
    }

    protected override void OnUpdate()
    {
    }
}