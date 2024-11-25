using System;
using System.Linq;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;

namespace Rendering
{
    public struct WorldSpriteSheetManager : IComponentData
    {
        public NativeArray<WorldSpriteSheetEntry> Entries;
        public float ColumnScale;
        public float RowScale;
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

            var singleton = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            singleton.ColumnScale = 1f / config.ColumnCount;
            singleton.RowScale = 1f / config.RowCount;

            var configEntryCount = config.SpriteSheetEntries.Length;
            var enumLength = GetMaxEnumValue<AnimationId>() + 1;

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

        private static int GetMaxEnumValue<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<int>() // Cast the enum values to integers
                .Max(); // Get the maximum value
        }
    }
}