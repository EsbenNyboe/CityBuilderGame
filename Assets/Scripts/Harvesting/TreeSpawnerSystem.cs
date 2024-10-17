using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class TreeSpawnerSystem : SystemBase
{
    private const int MaxHealth = 100;
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
        // var pathGrid = GridSetup.Instance.PathGrid;
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var walkableGrid = gridManager.WalkableGrid;
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        var cellSize = 1f; // gridManager currently only supports cellSize 1
        var damageableGrid = GridSetup.Instance.DamageableGrid;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;

        if (!_isInitialized)
        {
            _isInitialized = true;

            var areasToExclude = TreeGridSetup.AreasToExclude();

            for (var x = 0; x < gridWidth; x++)
            {
                for (var y = 0; y < gridHeight; y++)
                {
                    foreach (var areaToExclude in areasToExclude)
                    {
                        if (x >= areaToExclude.StartCell.x && x <= areaToExclude.EndCell.x &&
                            y >= areaToExclude.StartCell.y && y <= areaToExclude.EndCell.y)
                        {
                            continue;
                        }

                        var index = x * gridHeight + y;

                        var walkableCell = walkableGrid[index];
                        var gridDamageable = damageableGrid.GetGridObject(x, y);
                        TrySpawnTree(ref walkableCell, gridDamageable);
                        walkableGrid[index] = walkableCell;
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            PathingHelpers.GetXY(mousePosition, out var x, out var y);
            if (x > -1 && x < gridWidth && y > -1 && y < gridHeight)
            {
                _shouldSpawnTreesOnMouseDown = walkableGrid[PathingHelpers.GetIndex(gridManager.Height, x, y)].IsWalkable;
            }
        }

        if (!_shouldSpawnTreesOnMouseDown)
        {
            return;
        }

        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var brushSize = Globals.BrushSize();
            GridSetup.Instance.PathGrid.GetXY(mousePosition, out var x, out var y);
            var cellList = PathingHelpers.GetCellListAroundTargetCell(new int2(x, y), brushSize);

            for (var i = 0; i < cellList.Count; i++)
            {
                var index = cellList[i].x * gridHeight + cellList[i].y;
                var walkableCell = walkableGrid[index];
                var gridDamageable = damageableGrid.GetGridObject(cellList[i].x, cellList[i].y);
                TrySpawnTree(ref walkableCell, gridDamageable);
                walkableGrid[index] = walkableCell;
            }
        }
    }

    private void TrySpawnTree(ref WalkableCell walkableCell, GridDamageable gridDamageable)
    {
        if (gridDamageable == null)
        {
            return;
        }

        if (!walkableCell.IsWalkable || gridDamageable.IsDamageable())
        {
            return;
        }

        SpawnTreeWithoutEntity(ref walkableCell, gridDamageable);
    }

    private void SpawnTreeWithoutEntity(ref WalkableCell walkableCell, GridDamageable gridDamageable)
    {
        walkableCell.IsWalkable = false;
        gridDamageable.SetHealth(MaxHealth);
    }

    private bool GetNextValidGridPositionFromTop(ref int gridIndex, out int x, out int y)
    {
        var maxAttempts = 10;
        var currentAttempt = 0;
        x = 0;
        y = 0;

        while (currentAttempt < maxAttempts)
        {
            currentAttempt++;

            if (!GetNextGridPosition(gridIndex, out x, out y))
            {
                Debug.Log("No valid grid position found: Outside range");
                return false;
            }

            gridIndex++;
            if (ValidateWalkableGridPosition(x, y))
            {
                return true;
            }
        }

        Debug.Log("No valid grid position found: Not walkable");
        return false;
    }

    private bool GetNextGridPosition(int gridIndex, out int x, out int y)
    {
        var width = GridSetup.Instance.PathGrid.GetWidth();
        var maxY = GridSetup.Instance.PathGrid.GetHeight() - 1;

        x = width - 1 - gridIndex % width;
        y = maxY - gridIndex / width;

        return y >= 0;
    }

    private bool ValidateWalkableGridPosition(int x, int y)
    {
        return GridSetup.Instance.PathGrid.GetGridObject(x, y).IsWalkable();
    }
}