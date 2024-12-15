using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public partial struct GridManager
    {
        #region OccupiableGrid Core

        public bool IsOccupied(int gridIndex, Entity askingEntity = default)
        {
            if (!TryGetOccupant(gridIndex, out var occupant))
            {
                return false;
            }

            if (askingEntity != default && askingEntity == occupant)
            {
                // It is occupied by the asking entity. Therefore, we will consider this "not occupied".
                return false;
            }

            return true;

            // TODO: Find a way to check if entity exists, without using managed code (World)
            // && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(occupant);
        }

        public bool TryGetOccupant(int i, out Entity occupant)
        {
            occupant = GetOccupant(i);
            return occupant != Entity.Null;
        }

        public Entity GetOccupant(Vector3 position)
        {
            var cell = GridHelpers.GetXY(position);
            return GetOccupant(cell);
        }

        public Entity GetOccupant(int2 cell)
        {
            var i = GetIndex(cell);
            return GetOccupant(i);
        }

        private Entity GetOccupant(int i)
        {
            return OccupiableGrid[i].Occupant;
        }

        public bool EntityIsOccupant(int i, Entity entity)
        {
            return OccupiableGrid[i].Occupant == entity;
        }

        // Note: Remember to call SetComponent after this method
        public bool TryClearOccupant(int i, Entity entity)
        {
            if (EntityIsOccupant(i, entity))
            {
                SetOccupant(i, Entity.Null);
                return true;
            }

            return false;
        }

        // Note: Remember to call SetComponent after this method
        public void SetOccupant(int i, Entity entity)
        {
            var occupiableCell = OccupiableGrid[i];
            occupiableCell.Occupant = entity;
            occupiableCell.IsDirty = true;
            OccupiableGrid[i] = occupiableCell;
            OccupiableGridIsDirty = true;
        }

        #endregion

        #region OccupiableGrid Variants

        public bool IsOccupied(Vector3 position, Entity askingEntity = default)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return IsOccupied(x, y, askingEntity);
        }

        public bool IsOccupied(int2 cell, Entity askingEntity = default)
        {
            return IsOccupied(cell.x, cell.y, askingEntity);
        }

        public bool IsOccupied(int x, int y, Entity askingEntity = default)
        {
            var gridIndex = GetIndex(x, y);
            return IsOccupied(gridIndex, askingEntity);
        }

        public bool TryGetOccupant(Vector3 position, out Entity occupant)
        {
            var gridIndex = GetIndex(position);
            return TryGetOccupant(gridIndex, out occupant);
        }

        public bool TryGetOccupant(int2 cellPosition, out Entity occupant)
        {
            var gridIndex = GetIndex(cellPosition);
            return TryGetOccupant(gridIndex, out occupant);
        }

        public bool EntityIsOccupant(Vector3 position, Entity entity)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return EntityIsOccupant(x, y, entity);
        }

        public bool EntityIsOccupant(int2 cell, Entity entity)
        {
            return EntityIsOccupant(cell.x, cell.y, entity);
        }

        public bool EntityIsOccupant(int x, int y, Entity entity)
        {
            var gridIndex = GetIndex(x, y);
            return EntityIsOccupant(gridIndex, entity);
        }


        // Note: Remember to call SetComponent after this method
        public bool TryClearOccupant(Vector3 position, Entity entity)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return TryClearOccupant(x, y, entity);
        }

        // Note: Remember to call SetComponent after this method
        public bool TryClearOccupant(int2 cell, Entity entity)
        {
            return TryClearOccupant(cell.x, cell.y, entity);
        }

        // Note: Remember to call SetComponent after this method
        public bool TryClearOccupant(int x, int y, Entity entity)
        {
            var i = GetIndex(x, y);
            return TryClearOccupant(i, entity);
        }

        // Note: Remember to call SetComponent after this method
        public void SetOccupant(Vector3 position, Entity entity)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetOccupant(x, y, entity);
        }

        // Note: Remember to call SetComponent after this method
        public void SetOccupant(int2 cell, Entity entity)
        {
            SetOccupant(cell.x, cell.y, entity);
        }

        // Note: Remember to call SetComponent after this method
        public void SetOccupant(int x, int y, Entity entity)
        {
            var gridIndex = GetIndex(x, y);
            SetOccupant(gridIndex, entity);
        }

        #endregion
    }
}

public struct OccupiableCell
{
    public Entity Occupant;
    public bool IsDirty;
}