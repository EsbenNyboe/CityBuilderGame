using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public struct DamageableCell
    {
        public float Health;
        public float MaxHealth;
        public bool IsDirty;
    }

    public partial struct GridManager
    {
        #region DamageableGrid Core

        public float GetHealthNormalized(int i)
        {
            return DamageableGrid[i].Health / DamageableGrid[i].MaxHealth;
        }

        public bool IsDamageable(int i)
        {
            return DamageableGrid[i].Health > 0;
        }

        // Note: Remember to call SetComponent after this method
        public void SetHealthToMax(int i)
        {
            SetHealth(i, DamageableGrid[i].MaxHealth);
        }

        // Note: Remember to call SetComponent after this method
        public void SetHealthToZero(int i)
        {
            SetHealth(i, 0);
        }

        // Note: Remember to call SetComponent after this method
        public void AddDamage(int i, float damage)
        {
            SetHealth(i, DamageableGrid[i].Health - damage);
        }

        // Note: Remember to call SetComponent after this method
        public void SetHealth(int i, float health)
        {
            var damageableCell = DamageableGrid[i];
            damageableCell.Health = health;
            damageableCell.IsDirty = true;
            DamageableGrid[i] = damageableCell;
            DamageableGridIsDirty = true;
        }

        #endregion

        #region DamageableGrid Variants

        public bool IsDamageable(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            return IsDamageable(x, y);
        }

        public bool IsDamageable(int2 cell)
        {
            return IsDamageable(cell.x, cell.y);
        }

        public bool IsDamageable(int x, int y)
        {
            var i = GetIndex(x, y);
            return IsDamageable(i);
        }

        public void SetHealthToMax(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetHealthToMax(x, y);
        }

        public void SetHealthToMax(int2 cell)
        {
            SetHealthToMax(cell.x, cell.y);
        }

        public void SetHealthToMax(int x, int y)
        {
            var i = GetIndex(x, y);
            SetHealthToMax(i);
        }

        public void SetHealthToZero(Vector3 position)
        {
            GridHelpers.GetXY(position, out var x, out var y);
            SetHealthToZero(x, y);
        }

        public void SetHealthToZero(int2 cell)
        {
            SetHealthToZero(cell.x, cell.y);
        }

        public void SetHealthToZero(int x, int y)
        {
            var i = GetIndex(x, y);
            SetHealthToZero(i);
        }

        // Note: Remember to call SetComponent after this method
        public void SetHealth(Vector3 position, float health)
        {
            var cell = GridHelpers.GetXY(position);
            SetHealth(cell, health);
        }

        // Note: Remember to call SetComponent after this method
        public void SetHealth(int2 cell, float health)
        {
            var i = GetIndex(cell.x, cell.y);
            SetHealth(i, health);
        }

        #endregion
    }
}