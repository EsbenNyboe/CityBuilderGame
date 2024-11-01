using System;
using UnityEngine;

public class InteractableGridDebugVisual : GridVisual
{
    protected override bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
        ref Vector3 worldPosition)
    {
        var interactableCell = gridManager.InteractableGrid[index];
        if (!interactableCell.IsDirty)
        {
            uv00 = default;
            uv11 = default;
            quadSize = default;
            return false;
        }

        interactableCell.IsDirty = false;
        gridManager.InteractableGrid[index] = interactableCell;

        var frameOffset = interactableCell.InteractableCellType switch
        {
            InteractableCellType.None => 1f,
            InteractableCellType.Bed => 0f,
            _ => throw new ArgumentOutOfRangeException()
        };

        var frameSize = 1f;
        uv00 = new Vector2(frameOffset, 0);
        uv11 = new Vector2(frameOffset + frameSize, 1);

        return true;
    }
}