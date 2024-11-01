using UnityEngine;

public class PathGridVisual : GridVisual
{
    protected override bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11,
        ref Vector3 quadSize, ref Vector3 worldPosition)
    {
        var walkableCell = gridManager.WalkableGrid[index];
        if (!walkableCell.IsDirty)
        {
            uv00 = default;
            uv11 = default;
            quadSize = default;
            return false;
        }

        // Note: This shouldn't be set to dirty, because:
        // A: It's a static background
        // B: There's another grid-visual setting it to dirty (debug-grid)

        uv00 = new Vector2(0, 0);
        uv11 = new Vector2(.5f, .5f);

        if (!walkableCell.IsWalkable)
        {
            uv00 = new Vector2(.5f, .5f);
            uv11 = new Vector2(1f, 1f);
        }

        return true;
    }
}