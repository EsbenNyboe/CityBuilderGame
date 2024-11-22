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
    Idle,
    Walk,
    IdleHolding,
    WalkHolding,
    Talk,
    Sleep,
    ItemWood,
    ItemMeat,
    ItemBerries
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
        var rowCount = singleton.SpriteRows = config.SpriteRows;
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
                    singleton.AnimationConfigs[(int)AnimationId.Talk] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.Sleep:
                    singleton.AnimationConfigs[(int)AnimationId.Sleep] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.IdleHolding:
                    singleton.AnimationConfigs[(int)AnimationId.IdleHolding] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.WalkHolding:
                    singleton.AnimationConfigs[(int)AnimationId.WalkHolding] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.Walk:
                    singleton.AnimationConfigs[(int)AnimationId.Walk] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.Idle:
                    singleton.AnimationConfigs[(int)AnimationId.Idle] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.ItemWood:
                    singleton.AnimationConfigs[(int)AnimationId.ItemWood] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.ItemMeat:
                    singleton.AnimationConfigs[(int)AnimationId.ItemMeat] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case AnimationId.ItemBerries:
                    singleton.AnimationConfigs[(int)AnimationId.ItemBerries] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (animationConfig.Identifier)
            {
                case AnimationId.None:
                    break;
            }
        }

        SystemAPI.SetSingleton(singleton);
    }

    private AnimationConfig ReverseSpriteRow(AnimationConfig animationConfig, int rowCount)
    {
        animationConfig.SpriteRow = rowCount - animationConfig.SpriteRow - 1;
        return animationConfig;
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