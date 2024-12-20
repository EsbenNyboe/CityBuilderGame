using System;
using Inventory;
using Unity.Collections;
using Unity.Entities;
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
}