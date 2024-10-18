using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct GridManager
{
    #region Generic

    public void GetXY(int i, out int x, out int y)
    {
        x = i / Height;
        y = i % Height;
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

    #endregion

    #region Walkable

    public bool IsWalkable(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return IsWalkable(x, y);
    }

    public bool IsWalkable(int2 cell)
    {
        return IsWalkable(cell.x, cell.y);
    }

    public bool IsWalkable(int x, int y)
    {
        return IsWalkable(GetIndex(x, y));
    }

    public bool IsWalkable(int i)
    {
        return WalkableGrid[i].IsWalkable;
    }

    public void SetIsWalkable(Vector3 position, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        GridHelpers.GetXY(position, out var x, out var y);
        SetIsWalkable(x, y, isWalkable);
    }

    public void SetIsWalkable(int2 cell, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        SetIsWalkable(cell.x, cell.y, isWalkable);
    }

    public void SetIsWalkable(int x, int y, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        var gridIndex = GetIndex(x, y);
        SetIsWalkable(gridIndex, isWalkable);
    }

    public void SetIsWalkable(int i, bool isWalkable)
    {
        // Note: Remember to call SetComponent after this method
        var walkableCell = WalkableGrid[i];
        walkableCell.IsWalkable = isWalkable;
        walkableCell.IsDirty = true;
        WalkableGrid[i] = walkableCell;
        WalkableGridIsDirty = true;
    }

    #endregion

    #region OccupiableGrid

    public bool IsOccupied(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return IsOccupied(x, y);
    }

    public bool IsOccupied(int2 cell)
    {
        return IsOccupied(cell.x, cell.y);
    }

    public bool IsOccupied(int x, int y)
    {
        var gridIndex = GetIndex(x, y);
        return IsOccupied(gridIndex);
    }

    public bool IsOccupied(int gridIndex)
    {
        var occupant = OccupiableGrid[gridIndex].Occupant;
        return occupant != Entity.Null && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(occupant);
    }

    public bool EntityIsOccupant(int i, Entity entity)
    {
        return OccupiableGrid[i].Occupant == entity;
    }

    public bool TryClearOccupant(Vector3 position, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        GridHelpers.GetXY(position, out var x, out var y);
        return TryClearOccupant(x, y, entity);
    }

    public bool TryClearOccupant(int2 cell, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        return TryClearOccupant(cell.x, cell.y, entity);
    }

    public bool TryClearOccupant(int x, int y, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var i = GetIndex(x, y);
        return TryClearOccupant(i, entity);
    }

    public bool TryClearOccupant(int i, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        if (EntityIsOccupant(i, entity))
        {
            SetOccupant(i, Entity.Null);
            return true;
        }

        return false;
    }

    public void SetOccupant(int x, int y, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var gridIndex = GetIndex(x, y);
        SetOccupant(gridIndex, entity);
    }

    public void SetOccupant(int i, Entity entity)
    {
        // Note: Remember to call SetComponent after this method
        var occupiableCell = OccupiableGrid[i];
        occupiableCell.Occupant = entity;
        occupiableCell.IsDirty = true;
        OccupiableGrid[i] = occupiableCell;
        OccupiableGridIsDirty = true;
    }

    #endregion

    #region DamageableGrid

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

    public bool IsDamageable(int i)
    {
        return DamageableGrid[i].Health > 0;
    }

    public void SetHealthToMax(int i)
    {
        // Note: Remember to call SetComponent after this method
        SetHealth(i, DamageableGrid[i].MaxHealth);
    }

    public void AddDamage(int i, float damage)
    {
        // Note: Remember to call SetComponent after this method
        SetHealth(i, DamageableGrid[i].Health - damage);
    }

    public void SetHealth(int i, float health)
    {
        // Note: Remember to call SetComponent after this method
        var damageableCell = DamageableGrid[i];
        damageableCell.Health = health;
        damageableCell.IsDirty = true;
        DamageableGrid[i] = damageableCell;
        DamageableGridIsDirty = true;
    }

    #endregion
}