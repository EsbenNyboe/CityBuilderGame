using System;
using Unity.Entities;

public struct UnitAnimationManager : IComponentData
{
    public AnimationConfigz SleepAnimation;
    public AnimationConfigz WalkAnimation;
    public AnimationConfigz IdleAnimation;
    public int SpriteColumns;
    public int SpriteRows;
}

[Serializable]
public struct AnimationConfigz
{
    public int SpriteRow;
    public int FrameCount;
    public float FrameInterval;
}

public partial class UnitAnimationManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        var unitAnimationManager = new UnitAnimationManager
        {
            SleepAnimation = new AnimationConfigz
            {
                SpriteRow = 0,
                FrameCount = 3,
                FrameInterval = 0.4f
            },
            WalkAnimation = new AnimationConfigz
            {
                SpriteRow = 1,
                FrameCount = 2,
                FrameInterval = 0.11f
            },
            IdleAnimation = new AnimationConfigz
            {
                SpriteRow = 2,
                FrameCount = 2,
                FrameInterval = 0.8f
            },
            SpriteColumns = 3,
            SpriteRows = 3
        };
        EntityManager.AddComponent<UnitAnimationManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, unitAnimationManager);
    }

    protected override void OnUpdate()
    {
    }
}