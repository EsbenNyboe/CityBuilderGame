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
        var height = 200;

        var grid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < grid.Length; i++)
        {
            var cell = grid[i];
            cell.IsWalkable = false;
            cell.IsDirty = true;
            grid[i] = cell;
        }

        // RequireForUpdate<GridManager>();
        // var entity = EntityManager.CreateSingleton<GridManager>();

        // SystemAPI.SetComponent(entity, new GridManager
        // {
        //     Width = width,
        //     Height = height,
        //     WalkableGrid = grid
        // });

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

        // var gridManager = SystemAPI.GetSingleton<GridManager>();
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
        // var gridManager = SystemAPI.GetSingleton<GridManager>();
        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);
        Debug.Log("Test: " + gridManager.WalkableGrid[0].IsWalkable);
        Debug.Log("Test2: " + gridManager.WalkableGridIsDirty);
        gridManager.WalkableGrid.Dispose();
    }
}

// public partial struct GridUnmanagedSystem : ISystem
// {
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<GridManager>();
//         var entity = state.EntityManager.CreateSingleton<GridManager>();
//         state.EntityManager.AddComponent<GridManager>(new SystemHandle());
//
//         var width = 100;
//         var height = 200;
//
//         var grid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
//         for (var i = 0; i < grid.Length; i++)
//         {
//             var cell = grid[i];
//             cell.IsWalkable = true;
//             grid[i] = cell;
//         }
//
//         SystemAPI.SetComponent(entity, new GridManager
//         {
//             Width = width,
//             Height = height,
//             WalkableGrid = grid
//         });
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         if (!Input.GetKeyDown(KeyCode.Return))
//         {
//             return;
//         }
//
//         var gridUnmanaged = SystemAPI.GetSingleton<GridManager>();
//
//         for (var i = 0; i < gridUnmanaged.WalkableGrid.Length; i++)
//         {
//             var cell = gridUnmanaged.WalkableGrid[i];
//             cell.IsWalkable = cell.IsWalkable;
//             cell.IsDirty = true;
//             gridUnmanaged.WalkableGrid[i] = cell;
//         }
//     }
//
//     public void OnDestroy(ref SystemState state)
//     {
//         var gridUnmanaged = SystemAPI.GetSingleton<GridManager>();
//         gridUnmanaged.WalkableGrid.Dispose();
//     }
// }