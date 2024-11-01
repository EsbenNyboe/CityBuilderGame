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

    private bool IsEmptyCell(int2 cell)
    {
        return IsWalkable(cell) && !IsOccupied(cell) && !IsInteractable(cell);
    }

    private bool IsVacantCell(int2 cell)
    {
        return IsWalkable(cell) && !IsOccupied(cell);
    }

    #endregion
}