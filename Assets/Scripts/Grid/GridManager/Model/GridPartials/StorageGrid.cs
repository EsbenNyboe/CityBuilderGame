using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public struct StorageCell
    {
        public int ItemCapacity;
        public int ItemCount;
    }

    public partial struct GridManager
    {
        #region StorageGrid Core

        public int GetStorageItemCount(int i)
        {
            return StorageGrid[i].ItemCount;
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(int i, int itemCount)
        {
            var storageCell = StorageGrid[i];
            storageCell.ItemCount = itemCount;
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

        public int GetStorageItemCount(float3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return GetStorageItemCount(x, y);
        }

        public int GetStorageItemCount(int2 cell)
        {
            return GetStorageItemCount(cell.x, cell.y);
        }

        public int GetStorageItemCount(int x, int y)
        {
            return GetStorageItemCount(GetIndex(x, y));
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(Vector3 position, int itemCount)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetStorageCount(x, y, itemCount);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(int2 cell, int itemCount)
        {
            SetStorageCount(cell.x, cell.y, itemCount);
        }

        // Note: Remember to call SetComponent after this method
        public void SetStorageCount(int x, int y, int itemCount)
        {
            var gridIndex = GetIndex(x, y);
            SetStorageCount(gridIndex, itemCount);
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