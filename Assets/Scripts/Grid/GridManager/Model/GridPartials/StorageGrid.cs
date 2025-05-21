using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public struct StorageCell
    {
        public int ItemCount;
        public bool IsDirty;
    }

    public partial struct GridManager
    {
        #region StorageGrid Core

        public int GetStorageItemCount(int i)
        {
            return StorageGrid[i].ItemCount;
        }

        // Note: Remember to call SetComponent after this method
        public void SetItemCount(int i, int itemCount)
        {
            var storageCell = StorageGrid[i];
            storageCell.ItemCount = itemCount;
            storageCell.IsDirty = true;
            StorageGrid[i] = storageCell;
            StorageGridIsDirty = true;
        }

        #endregion

        #region StorageGrid Variants

        public int GetStorageItemCount(Vector3 position)
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
        public void SetItemCount(Vector3 position, int itemCount)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetItemCount(x, y, itemCount);
        }

        // Note: Remember to call SetComponent after this method
        public void SetItemCount(int2 cell, int itemCount)
        {
            SetItemCount(cell.x, cell.y, itemCount);
        }

        // Note: Remember to call SetComponent after this method
        public void SetItemCount(int x, int y, int itemCount)
        {
            var gridIndex = GetIndex(x, y);
            SetItemCount(gridIndex, itemCount);
        }

        #endregion
    }
}