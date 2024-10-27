using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct GridManager
{
    #region Generic Grid Helpers

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

    #region WalkableGrid Helpers

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

    // Note: Remember to call SetComponent after this method
    public void SetIsWalkable(Vector3 position, bool isWalkable)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        SetIsWalkable(x, y, isWalkable);
    }

    // Note: Remember to call SetComponent after this method
    public void SetIsWalkable(int2 cell, bool isWalkable)
    {
        SetIsWalkable(cell.x, cell.y, isWalkable);
    }

    // Note: Remember to call SetComponent after this method
    public void SetIsWalkable(int x, int y, bool isWalkable)
    {
        var gridIndex = GetIndex(x, y);
        SetIsWalkable(gridIndex, isWalkable);
    }

    #endregion

    #region OccupiableGrid Helpers

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
    public void SetOccupant(int x, int y, Entity entity)
    {
        var gridIndex = GetIndex(x, y);
        SetOccupant(gridIndex, entity);
    }

    #endregion

    #region DamageableGrid Helpers

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

    #endregion

    #region InteractableGrid Helpers

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
        var i = GetIndex(x, y);
        return IsInteractable(i);
    }


    public bool IsInteractedWith(Vector3 position)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return IsInteractedWith(x, y);
    }

    public bool IsInteractedWith(int2 cell)
    {
        return IsInteractedWith(cell.x, cell.y);
    }

    public bool IsInteractedWith(int x, int y)
    {
        var i = GetIndex(x, y);
        return IsInteractedWith(i);
    }


    // Note: Remember to call SetComponent after this method
    public void SetInteractable(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        SetInteractable(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(int2 cell, Entity entity)
    {
        SetInteractable(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractable(int x, int y, Entity entity)
    {
        var gridIndex = GetIndex(x, y);
        SetInteractable(gridIndex, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        SetInteractor(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(int2 cell, Entity entity)
    {
        SetInteractor(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public void SetInteractor(int x, int y, Entity entity)
    {
        var gridIndex = GetIndex(x, y);
        SetInteractor(gridIndex, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(Vector3 position, Entity entity)
    {
        GridHelpers.GetXY(position, out var x, out var y);
        return TryClearInteractor(x, y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(int2 cell, Entity entity)
    {
        return TryClearInteractor(cell.x, cell.y, entity);
    }

    // Note: Remember to call SetComponent after this method
    public bool TryClearInteractor(int x, int y, Entity entity)
    {
        var i = GetIndex(x, y);
        return TryClearInteractor(i, entity);
    }

    #endregion
}