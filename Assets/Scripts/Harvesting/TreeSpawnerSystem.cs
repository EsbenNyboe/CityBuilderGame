using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class TreeSpawnerSystem : SystemBase
{
    private static bool _shouldSpawnTreesOnMouseDown;
    private static bool _isInitialized;
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _isInitialized = false;
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var walkableGrid = gridManager.WalkableGrid;
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        var cellSize = 1f; // gridManager currently only supports cellSize 1
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;

        if (!_isInitialized)
        {
            _isInitialized = true;

            var areasToExclude = TreeGridSetup.AreasToExclude();

            for (var x = 0; x < gridWidth; x++)
            {
                for (var y = 0; y < gridHeight; y++)
                {
                    if (ExcludeCell(areasToExclude, x, y))
                    {
                        continue;
                    }

                    var index = GridHelpers.GetIndex(gridManager, x, y);
                    TrySpawnTree(gridManager, index);
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            GridHelpers.GetXY(mousePosition, out var x, out var y);
            if (x > -1 && x < gridWidth && y > -1 && y < gridHeight)
            {
                _shouldSpawnTreesOnMouseDown = walkableGrid[GridHelpers.GetIndex(gridManager.Height, x, y)].IsWalkable;
            }
        }

        if (!_shouldSpawnTreesOnMouseDown)
        {
            return;
        }

        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var brushSize = Globals.BrushSize();
            GridHelpers.GetXY(mousePosition, out var x, out var y);
            var cellList = GridHelpers.GetCellListAroundTargetCell(new int2(x, y), brushSize);

            for (var i = 0; i < cellList.Count; i++)
            {
                var index = cellList[i].x * gridHeight + cellList[i].y;
                TrySpawnTree(gridManager, index);
            }
        }
    }

    private static bool ExcludeCell(TreeGridSetup.AreaToExclude[] areasToExclude, int x, int y)
    {
        foreach (var areaToExclude in areasToExclude)
        {
            if (x >= areaToExclude.StartCell.x && x <= areaToExclude.EndCell.x &&
                y >= areaToExclude.StartCell.y && y <= areaToExclude.EndCell.y)
            {
                return true;
            }
        }

        return false;
    }

    private void TrySpawnTree(GridManager gridManager, int gridIndex)
    {
        if (!gridManager.WalkableGrid[gridIndex].IsWalkable || gridManager.DamageableGrid[gridIndex].Health > 0)
        {
            return;
        }

        SpawnTreeWithoutEntity(gridManager, gridIndex);
    }

    private void SpawnTreeWithoutEntity(GridManager gridManager, int gridIndex)
    {
        GridHelpers.SetIsWalkable(ref gridManager, gridIndex, false);
        GridHelpers.SetHealthToMax(ref gridManager, gridIndex);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }
}