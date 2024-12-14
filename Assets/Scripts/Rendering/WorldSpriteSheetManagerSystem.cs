using System;
using UnitState;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Utilities;

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

        public readonly Vector4 GetUvSingleFramed(WorldSpriteSheetEntryType type)
        {
            var entry = Entries[(int)type];
            return new Vector4
            {
                x = ColumnScale,
                y = RowScale,
                z = ColumnScale * entry.EntryColumns[0],
                w = RowScale * entry.EntryRows[0]
            };
        }

        public readonly void GetInventoryItemCoordinates(InventoryItem inventoryItem, out int column, out int row)
        {
            var entryType = inventoryItem switch
            {
                InventoryItem.None => WorldSpriteSheetEntryType.None,
                InventoryItem.LogOfWood => WorldSpriteSheetEntryType.ItemWood,
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

    public partial class WorldSpriteSheetManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<WorldSpriteSheetManager>();
        }

        protected override void OnUpdate()
        {
            var config = WorldSpriteSheetConfig.Instance;
            if (!config.IsDirty)
            {
                return;
            }

            config.IsDirty = false;

            ApplyConfigToSingleton(config);
        }

        protected override void OnDestroy()
        {
            var singleton = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            for (var index = 0; index < singleton.Entries.Length; index++)
            {
                var entry = singleton.Entries[index];
                entry.EntryColumns.Dispose();
                entry.EntryRows.Dispose();
                singleton.Entries[index] = entry;
            }

            singleton.Entries.Dispose();
            SystemAPI.SetSingleton(singleton);
        }

        private void ApplyConfigToSingleton(WorldSpriteSheetConfig config)
        {
            var singleton = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            singleton.ColumnScale = 1f / config.ColumnCount;
            singleton.RowScale = 1f / config.RowCount;

            var configEntryCount = config.SpriteSheetEntries.Length;
            var enumLength = EnumHelpers.GetMaxEnumValue<WorldSpriteSheetEntryType>() + 1;

            if (!singleton.Entries.IsCreated ||
                configEntryCount != singleton.Entries.Length)
            {
                singleton.Entries = new NativeArray<WorldSpriteSheetEntry>(enumLength, Allocator.Persistent);
            }

            var columnIndex = 0;
            var rowIndex = config.RowCount - 1;
            for (var i = 0; i < configEntryCount; i++)
            {
                var configEntry = config.SpriteSheetEntries[i];
                var singletonEntryIndex = configEntry.Identifier;
                var singletonEntry = singleton.Entries[(int)singletonEntryIndex];
                if (singletonEntry.EntryColumns.IsCreated)
                {
                    singletonEntry.EntryColumns.Dispose();
                    singletonEntry.EntryRows.Dispose();
                }

                singletonEntry.EntryColumns = new NativeArray<int>(configEntry.FrameCount, Allocator.Persistent);
                singletonEntry.EntryRows = new NativeArray<int>(configEntry.FrameCount, Allocator.Persistent);
                singletonEntry.FrameInterval = configEntry.FrameInterval;

                var frameIndex = 0;
                while (frameIndex < configEntry.FrameCount)
                {
                    singletonEntry.EntryColumns[frameIndex] = columnIndex;
                    singletonEntry.EntryRows[frameIndex] = rowIndex;
                    frameIndex++;
                    columnIndex++;
                    if (columnIndex >= config.ColumnCount)
                    {
                        columnIndex = 0;
                        rowIndex--;
                        Assert.IsTrue(rowIndex >= 0 || frameIndex >= configEntry.FrameCount,
                            "SpriteSheetEntry has invalid setup: Not enough rows!");
                    }
                }

                singleton.Entries[(int)singletonEntryIndex] = singletonEntry;
            }

            SystemAPI.SetSingleton(singleton);
        }
    }
}