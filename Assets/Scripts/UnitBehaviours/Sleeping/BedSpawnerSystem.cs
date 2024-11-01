using CodeMonkey.Utils;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class BedSpawnerSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _gridManagerSystemHandle = EntityManager.World.GetExistingSystem(typeof(GridManagerSystem));
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var cellSize = 1f; // gridManager currently only supports cellSize 1
            var mousePos = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;
            GridHelpers.GetXY(mousePos, out var x, out var y);
            gridManager.ValidateGridPosition(ref x, ref y);
            if (gridManager.IsWalkable(x, y) && !gridManager.IsInteractable(x, y))
            {
                gridManager.SetInteractableBed(x, y);
            }
            else if (gridManager.IsInteractable(x, y))
            {
                gridManager.SetInteractableNone(x, y);
                gridManager.SetIsWalkable(x, y, true);
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }
    }
}