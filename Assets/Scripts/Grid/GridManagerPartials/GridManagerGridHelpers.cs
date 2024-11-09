using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct GridManager
{
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

    #region Combined Grid Helpers

    public bool IsBedAvailableToUnit(float3 unitPosition, Entity unit)
    {
        var cell = GridHelpers.GetXY(unitPosition);
        return IsPositionInsideGrid(cell) && IsBed(cell) && !IsOccupied(cell, unit) && IsWalkable(cell);
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

    private bool IsVacantCell(int2 cell, Entity askingEntity)
    {
        return IsPositionInsideGrid(cell) && IsWalkable(cell) && !IsOccupied(cell, askingEntity);
    }

    #endregion
}