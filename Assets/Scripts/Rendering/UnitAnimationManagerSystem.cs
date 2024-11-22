using System;
using Unity.Entities;
using UnityEngine;

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
    [Min(0)] public int SpriteRow;
    [Min(0)] public int FrameCount;
    [Min(0.01f)] public float FrameInterval;
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
        EntityManager.CreateSingleton<UnitAnimationManager>();
    }

    protected override void OnUpdate()
    {
        var singleton = SystemAPI.GetSingleton<UnitAnimationManager>();
        var config = SpriteSheetRendererManager.Instance;

        singleton.SpriteColumns = config.SpriteColumns;
        singleton.SpriteRows = config.SpriteRows;
        foreach (var animationConfig in config.AnimationConfigs)
        {
            switch (animationConfig.Identifier)
            {
                case AnimationId.None:
                    break;
                case AnimationId.Talk:
                    singleton.TalkAnimation = animationConfig;
                    break;
                case AnimationId.Sleep:
                    singleton.SleepAnimation = animationConfig;
                    break;
                case AnimationId.Walk:
                    singleton.WalkAnimation = animationConfig;
                    break;
                case AnimationId.Idle:
                    singleton.IdleAnimation = animationConfig;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        SystemAPI.SetSingleton(singleton);
    }
}