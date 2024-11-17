using Unity.Mathematics;
using UnityEngine;

public struct WalkableCell
{
    public bool IsWalkable;
    public bool IsDirty;
    public int Section;
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

    // Note: Remember to call SetComponent after this method
    public void SetWalkableSection(int2 cell, int sectionKey)
    {
        var gridIndex = GetIndex(cell);
        var walkableCell = WalkableGrid[gridIndex];
        walkableCell.Section = sectionKey;
        WalkableGrid[gridIndex] = walkableCell;
    }

    /// <summary>
    ///     True, if there is a path from "cell" to "otherCell".
    ///     False, if pathing is impossible.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="otherCell"></param>
    /// <returns></returns>
    public bool IsMatchingSection(int2 cell, int2 otherCell)
    {
        return GetSection(cell) == GetSection(otherCell);
    }

    private int GetSection(int2 cell)
    {
        var gridIndex = GetIndex(cell);
        return WalkableGrid[gridIndex].Section;
    }
}