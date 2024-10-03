﻿using CodeMonkey.Utils;
using Unity.Entities;
using UnityEngine;

public partial class TreeSpawnerSystem : SystemBase
{
    private const int MaxHealth = 100;
    private static bool _shouldSpawnTreesOnMouseDown;

    protected override void OnCreate()
    {
        RequireForUpdate<TreeSpawner>();
    }

    protected override void OnUpdate()
    {
        var treeSpawner = SystemAPI.GetSingleton<TreeSpawner>();
        var pathGrid = GridSetup.Instance.PathGrid;
        var damageableGrid = GridSetup.Instance.DamageableGrid;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathGrid.GetCellSize() * .5f;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var gridIndex = 0;
            for (var i = 0; i < treeSpawner.AmountToSpawn; i++)
            {
                if (GetNextValidGridPositionFromTop(ref gridIndex, out var x, out var y))
                {
                    var gridPath = pathGrid.GetGridObject(x, y);
                    var gridDamageable = damageableGrid.GetGridObject(x, y);
                    TrySpawnTree(gridPath, gridDamageable);
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridNode = pathGrid.GetGridObject(mousePosition);
            if (gridNode == null)
            {
                return;
            }

            _shouldSpawnTreesOnMouseDown = gridNode.IsWalkable();
        }

        if (!_shouldSpawnTreesOnMouseDown)
        {
            return;
        }

        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridPath = pathGrid.GetGridObject(mousePosition);
            var gridDamageable = damageableGrid.GetGridObject(mousePosition);
            TrySpawnTree(gridPath, gridDamageable);
        }
    }

    private void TrySpawnTree(GridPath gridPath, GridDamageable gridDamageable)
    {
        if (gridPath == null || gridDamageable == null)
        {
            return;
        }

        if (!gridPath.IsWalkable() || gridDamageable.IsDamageable())
        {
            return;
        }

        SpawnTreeWithoutEntity(gridPath, gridDamageable);
    }

    private void SpawnTreeWithoutEntity(GridPath gridPath, GridDamageable gridDamageable)
    {
        gridPath.SetIsWalkable(false);
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