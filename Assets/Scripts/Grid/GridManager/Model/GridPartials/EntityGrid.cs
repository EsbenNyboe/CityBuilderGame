using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public enum GridEntityType
    {
        None,
        Storage,
        Tree,
        House
    }

    public partial struct GridManager
    {
        #region EntityGrid Core

        private void AddGridEntity(int gridIndex, Entity entity, GridEntityType type)
        {
            Assert.IsTrue(GetGridEntity(gridIndex) == Entity.Null);
            SetGridEntity(gridIndex, entity, type);
        }

        public void RemoveGridEntity(int gridIndex)
        {
            Assert.IsTrue(GetGridEntity(gridIndex) != Entity.Null);
            SetGridEntity(gridIndex, Entity.Null, GridEntityType.None);
        }

        private void SetGridEntity(int gridIndex, Entity entity, GridEntityType type)
        {
            SetGridEntityType(gridIndex, type);
            SetGridEntity(gridIndex, entity);
        }

        private void SetGridEntityType(int gridIndex, GridEntityType gridEntityType)
        {
            GridEntityTypeGrid[gridIndex] = gridEntityType;
        }

        private void SetGridEntity(int gridIndex, Entity entity)
        {
            GridEntityGrid[gridIndex] = entity;
        }

        private bool TryGetGridEntityOfType(int gridIndex, GridEntityType type, out Entity entity)
        {
            entity = Entity.Null;
            if (GetGridEntityType(gridIndex) == type)
            {
                entity = GetGridEntity(gridIndex);
                Assert.IsTrue(entity != Entity.Null || type == GridEntityType.None);
                return true;
            }

            return false;
        }

        private bool TryGetGridEntityAndType(int gridIndex, out GridEntityType type, out Entity entity)
        {
            type = GetGridEntityType(gridIndex);
            entity = GetGridEntity(gridIndex);
            if (type != GridEntityType.None && entity != Entity.Null)
            {
                return true;
            }

            Debug.LogError("Cell has no Grid Entity!");
            return false;
        }

        private GridEntityType GetGridEntityType(int gridIndex)
        {
            return GridEntityTypeGrid[gridIndex];
        }

        private Entity GetGridEntity(int gridIndex)
        {
            return GridEntityGrid[gridIndex];
        }

        #endregion


        #region EntityGrid Variants

        public void RemoveGridEntity(Vector3 position)
        {
            var gridIndex = GetIndex(position);
            RemoveGridEntity(gridIndex);
        }

        public void RemoveGridEntity(int2 cell)
        {
            var gridIndex = GetIndex(cell);
            RemoveGridEntity(gridIndex);
        }

        public void AddGridEntity(Vector3 position, Entity entity, GridEntityType type)
        {
            var gridIndex = GetIndex(position);
            AddGridEntity(gridIndex, entity, type);
        }

        public void AddGridEntity(int2 cell, Entity entity, GridEntityType type)
        {
            var gridIndex = GetIndex(cell);
            AddGridEntity(gridIndex, entity, type);
        }

        public void SetGridEntity(Vector3 position, Entity entity, GridEntityType type)
        {
            var gridIndex = GetIndex(position);
            SetGridEntity(gridIndex, entity, type);
        }

        public void SetGridEntity(int2 cell, Entity entity, GridEntityType type)
        {
            var gridIndex = GetIndex(cell);
            SetGridEntity(gridIndex, entity, type);
        }

        public bool HasGridEntity(Vector3 position)
        {
            return !TryGetGridEntityOfType(position, GridEntityType.None, out _);
        }

        public bool HasGridEntity(int2 cell)
        {
            return !TryGetGridEntityOfType(cell, GridEntityType.None, out _);
        }

        public bool TryGetStorageEntity(Vector3 position, out Entity entity)
        {
            return TryGetGridEntityOfType(position, GridEntityType.Storage, out entity);
        }

        public bool TryGetStorageEntity(int2 cell, out Entity entity)
        {
            return TryGetGridEntityOfType(cell, GridEntityType.Storage, out entity);
        }

        public bool TryGetStorageEntity(int gridIndex, out Entity entity)
        {
            return TryGetGridEntityOfType(gridIndex, GridEntityType.Storage, out entity);
        }

        public bool TryGetHouseEntity(Vector3 position, out Entity entity)
        {
            return TryGetGridEntityOfType(position, GridEntityType.House, out entity);
        }

        public bool TryGetHouseEntity(int2 cell, out Entity entity)
        {
            return TryGetGridEntityOfType(cell, GridEntityType.House, out entity);
        }

        public bool TryGetHouseEntity(int gridIndex, out Entity entity)
        {
            return TryGetGridEntityOfType(gridIndex, GridEntityType.House, out entity);
        }

        public bool TryGetTreeEntity(Vector3 position, out Entity entity)
        {
            return TryGetGridEntityOfType(position, GridEntityType.Tree, out entity);
        }

        public bool TryGetTreeEntity(int2 cell, out Entity entity)
        {
            return TryGetGridEntityOfType(cell, GridEntityType.Tree, out entity);
        }

        public bool TryGetTreeEntity(int gridIndex, out Entity entity)
        {
            return TryGetGridEntityOfType(gridIndex, GridEntityType.Tree, out entity);
        }

        private bool TryGetGridEntityOfType(Vector3 position, GridEntityType type, out Entity entity)
        {
            var gridIndex = GetIndex(position);
            return TryGetGridEntityOfType(gridIndex, type, out entity);
        }

        private bool TryGetGridEntityOfType(int2 cell, GridEntityType type, out Entity entity)
        {
            var gridIndex = GetIndex(cell);
            return TryGetGridEntityOfType(gridIndex, type, out entity);
        }

        public bool TryGetGridEntityAndType(Vector3 position, out Entity entity, out GridEntityType type)
        {
            var gridIndex = GetIndex(position);
            return TryGetGridEntityAndType(gridIndex, out type, out entity);
        }

        public bool TryGetGridEntityAndType(int2 cell, out Entity entity, out GridEntityType type)
        {
            var gridIndex = GetIndex(cell);
            return TryGetGridEntityAndType(gridIndex, out type, out entity);
        }

        #endregion
    }
}