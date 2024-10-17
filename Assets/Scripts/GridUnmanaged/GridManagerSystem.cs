using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct GridManager : IComponentData
{
    public int Width;
    public int Height;
    public NativeArray<WalkableCell> WalkableGrid;
    public bool WalkableGridIsDirty;
}

public struct WalkableCell
{
    public bool IsWalkable;
    public bool IsDirty;
}

public partial class GridManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        var width = 100;
        var height = 100;

        var grid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < grid.Length; i++)
        {
            var cell = grid[i];
            cell.IsWalkable = true;
            cell.IsDirty = true;
            grid[i] = cell;
        }

        EntityManager.AddComponent<GridManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, new GridManager
        {
            Width = width,
            Height = height,
            WalkableGrid = grid,
            WalkableGridIsDirty = true
        });
    }

    protected override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }

        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);

        for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
        {
            var cell = gridManager.WalkableGrid[i];
            cell.IsWalkable = !cell.IsWalkable;
            cell.IsDirty = true;
            gridManager.WalkableGrid[i] = cell;
        }

        gridManager.WalkableGridIsDirty = true;
        SystemAPI.SetComponent(SystemHandle, gridManager);
    }

    protected override void OnDestroy()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);
        for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
        {
            // Debug.Log("IsWalkable: " + gridManager.WalkableGrid[i].IsWalkable);
        }

        gridManager.WalkableGrid.Dispose();
    }
}