using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public struct InteractableCell
    {
        public InteractableCellType InteractableCellType;
        public bool IsDirty;
    }

    public enum InteractableCellType
    {
        None,
        Bed
    }

    public partial struct GridManager
    {
        #region InteractableGrid Core

        public bool IsInteractable(int i)
        {
            return GetInteractableCellType(i) != InteractableCellType.None;
        }

        public bool IsBed(int i)
        {
            return GetInteractableCellType(i) == InteractableCellType.Bed;
        }

        private InteractableCellType GetInteractableCellType(int i)
        {
            return InteractableGrid[i].InteractableCellType;
        }

        public void SetInteractableNone(int i)
        {
            SetInteractableCellType(i, InteractableCellType.None);
        }

        public void SetInteractableBed(int i)
        {
            SetInteractableCellType(i, InteractableCellType.Bed);
        }

        public void SetInteractableCellType(int i, InteractableCellType type)
        {
            var interactableCell = InteractableGrid[i];
            interactableCell.InteractableCellType = type;
            interactableCell.IsDirty = true;
            InteractableGrid[i] = interactableCell;
            InteractableGridIsDirty = true;
        }

        #endregion

        #region InteractableGrid Variants

        public bool IsInteractable(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return IsInteractable(x, y);
        }

        public bool IsInteractable(int2 cell)
        {
            return IsInteractable(cell.x, cell.y);
        }

        public bool IsInteractable(int x, int y)
        {
            return IsInteractable(GetIndex(x, y));
        }

        public bool IsBed(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return IsBed(x, y);
        }

        public bool IsBed(int2 cell)
        {
            return IsBed(cell.x, cell.y);
        }

        public bool IsBed(int x, int y)
        {
            return IsBed(GetIndex(x, y));
        }

        public void SetInteractableBed(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetInteractableBed(x, y);
        }

        public void SetInteractableBed(int2 cell)
        {
            SetInteractableBed(cell.x, cell.y);
        }

        public void SetInteractableBed(int x, int y)
        {
            var gridIndex = GetIndex(x, y);
            SetInteractableBed(gridIndex);
        }

        public void SetInteractableNone(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetInteractableNone(x, y);
        }

        public void SetInteractableNone(int2 cell)
        {
            SetInteractableNone(cell.x, cell.y);
        }

        public void SetInteractableNone(int x, int y)
        {
            var gridIndex = GetIndex(x, y);
            SetInteractableNone(gridIndex);
        }

        public void SetInteractableCellType(Vector3 position, InteractableCellType type)
        {
            var cell = GridHelpers.GetXY(position);
            SetInteractableCellType(cell, type);
        }

        public void SetInteractableCellType(int2 cell, InteractableCellType type)
        {
            var i = GetIndex(cell);
            SetInteractableCellType(i, type);
        }

        #endregion
    }
}