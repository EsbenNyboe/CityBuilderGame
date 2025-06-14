using System;
using Inventory;
using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public struct StorageCell
    {
        public int ItemCapacity;
        public int ItemCountLog;
        public int ItemCountRawMeat;
        public int ItemCountCookedMeat;

        public readonly int ItemCount()
        {
            return ItemCountLog + ItemCountRawMeat + ItemCountCookedMeat;
        }
    }

    public partial struct GridManager
    {
        #region StorageGrid Core

        private int GetStorageItemCount(int i, InventoryItem item)
        {
            return item switch
            {
                InventoryItem.None => StorageGrid[i].ItemCount(),
                InventoryItem.LogOfWood => StorageGrid[i].ItemCountLog,
                InventoryItem.RawMeat => StorageGrid[i].ItemCountRawMeat,
                InventoryItem.CookedMeat => StorageGrid[i].ItemCountCookedMeat,
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
            };
        }

        // Note: Remember to call SetComponent after this method
        private void SetStorageCount(int i, int itemCount, InventoryItem item)
        {
            var storageCell = StorageGrid[i];
            switch (item)
            {
                case InventoryItem.None:
                    throw new ArgumentOutOfRangeException(nameof(item), item, null);
                case InventoryItem.LogOfWood:
                    storageCell.ItemCountLog = itemCount;
                    break;
                case InventoryItem.RawMeat:
                    storageCell.ItemCountRawMeat = itemCount;
                    break;
                case InventoryItem.CookedMeat:
                    storageCell.ItemCountCookedMeat = itemCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item), item, null);
            }

            StorageGrid[i] = storageCell;
        }

        public int GetStorageItemCapacity(int i)
        {
            return StorageGrid[i].ItemCapacity;
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCapacity(int i, int itemCapacity)
        {
            var storageCell = StorageGrid[i];
            storageCell.ItemCapacity = itemCapacity;
            StorageGrid[i] = storageCell;
        }

        #endregion

        #region ItemCount

        public int GetStorageItemCount(float3 position, InventoryItem item = InventoryItem.None)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return GetStorageItemCount(x, y, item);
        }

        public int GetStorageItemCount(int2 cell, InventoryItem item = InventoryItem.None)
        {
            return GetStorageItemCount(cell.x, cell.y, item);
        }

        private int GetStorageItemCount(int x, int y, InventoryItem item)
        {
            return GetStorageItemCount(GetIndex(x, y), item);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(Vector3 position, int itemCount, InventoryItem item)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetStorageCount(x, y, itemCount, item);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(int2 cell, int itemCount, InventoryItem item)
        {
            SetStorageCount(cell.x, cell.y, itemCount, item);
        }

        // Note: Remember to call SetComponent after this method
        private void SetStorageCount(int x, int y, int itemCount, InventoryItem item)
        {
            var gridIndex = GetIndex(x, y);
            SetStorageCount(gridIndex, itemCount, item);
        }

        #endregion

        #region ItemCapacity

        public int GetStorageItemCapacity(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return GetStorageItemCapacity(x, y);
        }

        public int GetStorageItemCapacity(int2 cell)
        {
            return GetStorageItemCapacity(cell.x, cell.y);
        }

        public int GetStorageItemCapacity(int x, int y)
        {
            return GetStorageItemCapacity(GetIndex(x, y));
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCapacity(Vector3 position, int itemCapacity)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetStorageCapacity(x, y, itemCapacity);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCapacity(int2 cell, int itemCapacity)
        {
            SetStorageCapacity(cell.x, cell.y, itemCapacity);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCapacity(int x, int y, int itemCapacity)
        {
            var gridIndex = GetIndex(x, y);
            SetStorageCapacity(gridIndex, itemCapacity);
        }

        #endregion

        public void SetDefaultStorageCapacity(Vector3 position)
        {
            SetDefaultStorageCapacity(GridHelpers.GetXY(position));
        }

        public void SetDefaultStorageCapacity(int2 cell)
        {
            SetDefaultStorageCapacity(GetIndex(cell));
        }

        public void SetDefaultStorageCapacity(int index)
        {
            SetStorageCapacity(index, 12);
        }
    }
}