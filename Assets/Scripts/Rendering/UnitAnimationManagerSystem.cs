using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct UnitAnimationManager : IComponentData
{
    public int SpriteColumns;
    public int SpriteRows;

    public NativeArray<AnimationConfig> AnimationConfigs;
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
        var config = SpriteSheetRendererManager.Instance;
        if (!config.IsDirty)
        {
            return;
        }

        config.IsDirty = false;

        var singleton = SystemAPI.GetSingleton<UnitAnimationManager>();

        singleton.SpriteColumns = config.SpriteColumns;
        singleton.SpriteRows = config.SpriteRows;
        if (singleton.AnimationConfigs.IsCreated)
        {
            singleton.AnimationConfigs.Dispose();
        }

        var maxEnum = GetMaxEnumValue<AnimationId>();
        singleton.AnimationConfigs =
            new NativeArray<AnimationConfig>(maxEnum + 1, Allocator.Persistent);
        foreach (var animationConfig in config.AnimationConfigs)
        {
            switch (animationConfig.Identifier)
            {
                case AnimationId.None:
                    break;
                case AnimationId.Talk:
                    singleton.AnimationConfigs[(int)AnimationId.Talk] = animationConfig;
                    break;
                case AnimationId.Sleep:
                    singleton.AnimationConfigs[(int)AnimationId.Sleep] = animationConfig;
                    break;
                case AnimationId.Walk:
                    singleton.AnimationConfigs[(int)AnimationId.Walk] = animationConfig;
                    break;
                case AnimationId.Idle:
                    singleton.AnimationConfigs[(int)AnimationId.Idle] = animationConfig;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        SystemAPI.SetSingleton(singleton);
    }

    protected override void OnDestroy()
    {
        var singleton = SystemAPI.GetSingleton<UnitAnimationManager>();
        singleton.AnimationConfigs.Dispose();
        SystemAPI.SetSingleton(singleton);
    }

    private static int GetMaxEnumValue<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<int>()  // Cast the enum values to integers
            .Max();       // Get the maximum value
    }
}