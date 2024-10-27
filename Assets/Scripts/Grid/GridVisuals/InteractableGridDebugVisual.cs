using UnityEngine;

public class InteractableGridDebugVisual : GridVisual
{
    protected override bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
        ref Vector3 worldPosition)
    {
        var colorGreen = 0.9f;
        uv00 = new Vector2(colorGreen, 0f);
        uv11 = new Vector2(colorGreen, 1f);

        var quadWidth = 0.5f;
        var quadHeight = 0.5f;

        var hasInteractor = gridManager.IsInteractedWith(index);
        quadSize = hasInteractor ? new Vector3(quadWidth, quadHeight) : Vector3.zero;
        worldPosition.x += 0.25f;

        return true;
    }
}