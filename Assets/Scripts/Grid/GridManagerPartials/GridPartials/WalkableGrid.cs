using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WalkableCell
{
    public bool IsWalkable;
    public bool IsDirty;
}

public partial struct GridManager
{
    #region WalkableGrid Core

    public bool IsWalkable(int i)
    {
        return WalkableGrid[i].IsWalkable;
    }

    // Note: Remember to call SetComponent after this method
    public void SetIsWalkable(int i, bool isWalkable)
    {
        var walkableCell = WalkableGrid[i];
        walkableCell.IsWalkable = isWalkable;
        walkableCell.IsDirty = true;
        WalkableGrid[i] = walkableCell;
        WalkableGridIsDirty = true;
    }

    #endregion

    #region WalkableGrid Variants

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
}