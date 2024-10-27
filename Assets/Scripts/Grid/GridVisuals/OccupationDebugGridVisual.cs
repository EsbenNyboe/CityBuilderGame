using UnityEngine;

public class OccupationDebugGridVisual : GridVisual
{
    protected override bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
        ref Vector3 worldPosition)
    {
        var cellSize = 1f; // GridManager currently only supports a cellSize of one

        var quadWidth = 0.5f;
        var quadHeight = 0.5f;
        quadSize = gridManager.IsOccupied(index) ? new Vector3(quadWidth, quadHeight) * cellSize : Vector3.zero;

        var colorRed = 0.1f;
        uv00 = new Vector2(colorRed, 0f);
        uv11 = new Vector2(colorRed, 1f);

        worldPosition.x -= 0.25f;

        return true;
    }
}