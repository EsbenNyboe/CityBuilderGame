using System;
using System.Linq;
using Rendering;
using UnitState;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct UnitAnimationManager : IComponentData
{
    public int SpriteColumns;
    public int SpriteRows;

    public NativeArray<AnimationConfig> AnimationConfigs;

    public int GetSpriteRowOfInventoryItem(InventoryItem item)
    {
        return item switch
        {
            InventoryItem.None => -1,
            InventoryItem.LogOfWood => AnimationConfigs[(int)WorldSpriteSheetEntryType.ItemWood].SpriteRow,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };
    }
}

[Serializable]
public struct AnimationConfig
{
    public WorldSpriteSheetEntryType Identifier;
    [Min(0)] public int SpriteRow;
    [Min(0)] public int FrameCount;
    [Min(0.01f)] public float FrameInterval;
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

        var maxEnum = GetMaxEnumValue<WorldSpriteSheetEntryType>();
        singleton.AnimationConfigs =
            new NativeArray<AnimationConfig>(maxEnum + 1, Allocator.Persistent);
        foreach (var animationConfig in config.AnimationConfigs)
        {
            switch (animationConfig.Identifier)
            {
                case WorldSpriteSheetEntryType.None:
                    break;
                case WorldSpriteSheetEntryType.Talk:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.Talk] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.Sleep:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.Sleep] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.IdleHolding:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.IdleHolding] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.WalkHolding:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.WalkHolding] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.Walk:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.Walk] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.Idle:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.Idle] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.ItemWood:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.ItemWood] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.ItemMeat:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.ItemMeat] = ReverseSpriteRow(animationConfig, rowCount);
                    break;
                case WorldSpriteSheetEntryType.ItemBerries:
                    singleton.AnimationConfigs[(int)WorldSpriteSheetEntryType.ItemBerries] =
                        ReverseSpriteRow(animationConfig, rowCount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (animationConfig.Identifier)
            {
                case WorldSpriteSheetEntryType.None:
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
            .Cast<int>() // Cast the enum values to integers
            .Max(); // Get the maximum value
    }
}