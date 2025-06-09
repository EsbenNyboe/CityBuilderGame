using System;
using Inventory;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    /// <summary>
    ///     Entries are ordered by the enum-ID they belong to, UNLIKE the entry-ordering of <see cref="WorldSpriteSheetConfig" />
    /// </summary>
    public struct WorldSpriteSheetManager : IComponentData
    {
        public NativeArray<WorldSpriteSheetEntry> Entries;
        public float ColumnScale;
        public float RowScale;

        public float2 EdibleOffset;

        public readonly Vector4 GetUv(WorldSpriteSheetEntryType type, int frame = 0)
        {
            var entry = Entries[(int)type];
            return new Vector4
            {
                x = ColumnScale,
                y = RowScale,
                z = ColumnScale * entry.EntryColumns[frame],
                w = RowScale * entry.EntryRows[frame]
            };
        }

        public readonly int GetAnimationLength(WorldSpriteSheetEntryType type)
        {
            return Entries[(int)type].EntryRows.Length;
        }

        public readonly float GetFrameInterval(WorldSpriteSheetEntryType type)
        {
            return Entries[(int)type].FrameInterval;
        }

        public readonly void GetInventoryItemCoordinates(InventoryItem inventoryItem, out int column, out int row)
        {
            var entryType = inventoryItem switch
            {
                InventoryItem.None => WorldSpriteSheetEntryType.None,
                InventoryItem.LogOfWood => WorldSpriteSheetEntryType.ItemWood,
                InventoryItem.CookedMeat => WorldSpriteSheetEntryType.ItemMeatCooked,
                InventoryItem.RawMeat => WorldSpriteSheetEntryType.ItemMeat,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryItem), inventoryItem, null)
            };

            var entryIndex = (int)entryType;
            if (entryIndex <= 0)
            {
                column = row = -1;
            }
            else
            {
                column = Entries[entryIndex].EntryColumns[0];
                row = Entries[entryIndex].EntryRows[0];
            }
        }

        public readonly bool IsInitialized()
        {
            return Entries.IsCreated;
        }
    }

    public struct WorldSpriteSheetEntry
    {
        public NativeArray<int> EntryColumns;
        public NativeArray<int> EntryRows;
        public float FrameInterval;
    }

    [Serializable]
    public struct SpriteSheetEntry
    {
        public WorldSpriteSheetEntryType Identifier;
        [Min(1)] public int FrameCount;
        [Min(0.001f)] public float FrameInterval;
    }

    public enum WorldSpriteSheetEntryType
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
        ItemBerries,
        Storage,
        BoarStand,
        BoarRun,
        BoarDead,
        SpearHolding,
        SpearThrowing,
        Spear,
        House,
        TreeDamaged,
        Tree,
        Bed,
        BonfireReady,
        BonfireBurning,
        BonfireDead,
        BabyIdle,
        BabyWalk,
        BabySleep,
        VillagerCookMeat,
        VillagerCookMeatDone,
        ItemMeatCooked,
        VillagerEat,
        BabyEat
    }
}