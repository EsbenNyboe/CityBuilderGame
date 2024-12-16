using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public partial struct GridManager
    {
        #region MonoBehaviour Helpers

        public bool IsInitialized()
        {
            return DamageableGrid.IsCreated;
        }

        #endregion

        #region Generic Grid Helpers

        public int2 GetXY(int i)
        {
            GetXY(i, out var x, out var y);
            return new int2(x, y);
        }

        public void GetXY(int i, out int x, out int y)
        {
            x = i / Height;
            y = i % Height;
        }

        public int GetIndex(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return GetIndex(x, y);
        }

        public int GetIndex(int2 cell)
        {
            return GetIndex(cell.x, cell.y);
        }

        public int GetIndex(int x, int y)
        {
            return x * Height + y;
        }

        public void ValidateGridPosition(ref int x, ref int y)
        {
            x = math.clamp(x, 0, Width - 1);
            y = math.clamp(y, 0, Height - 1);
        }

        public bool IsPositionInsideGrid(Vector3 position)
        {
            var cell = GridHelpers.GetXY(position);
            return IsPositionInsideGrid(cell.x, cell.y);
        }

        public bool IsPositionInsideGrid(int2 cell)
        {
            return IsPositionInsideGrid(cell.x, cell.y);
        }

        public bool IsPositionInsideGrid(int x, int y)
        {
            return
                x >= 0 &&
                y >= 0 &&
                x < Width &&
                y < Height;
        }

        public bool IsIndexInsideGrid(int index)
        {
            return index < Width * Height;
        }

        #endregion

        #region Combined Grid Helpers

        public bool IsBedAvailableToUnit(float3 unitPosition, Entity unit)
        {
            var cell = GridHelpers.GetXY(unitPosition);
            return IsBedAvailableToUnit(cell, unit);
        }

        public bool IsBedAvailableToUnit(int2 cell, Entity unit)
        {
            return IsPositionInsideGrid(cell) && IsBed(cell) && EntityIsOccupant(cell, unit) && IsWalkable(cell);
        }

        private bool TryClearBed(int i)
        {
            if (IsBed(i))
            {
                SetIsWalkable(i, true);
                return true;
            }

            return false;
        }

        public bool IsTree(int2 cell)
        {
            return IsPositionInsideGrid(cell) && IsDamageable(cell);
        }

        private bool IsAvailableBed(int2 cell)
        {
            return IsPositionInsideGrid(cell) && IsBed(cell) && !IsOccupied(cell) && IsWalkable(cell);
        }

        private bool IsEmptyCell(int2 cell)
        {
            return IsPositionInsideGrid(cell) && IsWalkable(cell) && !IsOccupied(cell) && !IsInteractable(cell);
        }

        private bool IsVacantCell(int2 cell, Entity askingEntity = default)
        {
            return IsPositionInsideGrid(cell) && IsWalkable(cell) && !IsOccupied(cell, askingEntity);
        }

        public void OnUnitDestroyed(Entity entity, Vector3 position)
        {
            var gridIndex = GetIndex(position);
            OnUnitDestroyed(entity, gridIndex);
        }

        public void OnUnitDestroyed(Entity entity, int2 cell)
        {
            var gridIndex = GetIndex(cell);
            OnUnitDestroyed(entity, gridIndex);
        }

        public void OnUnitDestroyed(Entity entity, int i)
        {
            if (!IsIndexInsideGrid(i))
            {
                return;
            }

            if (TryClearOccupant(i, entity))
            {
                TryClearBed(i);
            }
        }

        #endregion
    }
}