using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class SpawnCubesSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnCubesConfig>();
    }

    protected override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        var spawnCubesConfig = SystemAPI.GetSingleton<SpawnCubesConfig>();

        var gridIndex = 0;
        for (var i = 0; i < spawnCubesConfig.AmountToSpawn; i++)
        {
            if (!GetNextValidGridPosition(ref gridIndex, out var x, out var y))
            {
                return;
            }

            var spawnedEntity = EntityManager.Instantiate(spawnCubesConfig.ObjectToSpawn);
            EntityManager.SetComponentData(spawnedEntity, new LocalTransform
            {
                Position = new float3
                {
                    x = x,
                    y = y,
                    z = -0.01f
                },
                Scale = 1f,
                Rotation = quaternion.identity
            });

            GridSetup.Instance.OccupationGrid.GetGridObject(x, y).SetOccupied(spawnedEntity);
        }
    }

    private bool GetNextValidGridPosition(ref int gridIndex, out int x, out int y)
    {
        var maxAttempts = 5000;
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

            if (ValidateWalkableGridPosition(x, y) && ValidateVacantGridPosition(x, y))
            {
                return true;
            }

            gridIndex++;
        }

        Debug.Log("No valid grid position found: Not walkable");
        return false;
    }

    private bool GetNextGridPosition(int gridIndex, out int x, out int y)
    {
        var width = GridSetup.Instance.PathGrid.GetWidth();
        var maxY = GridSetup.Instance.PathGrid.GetHeight() - 1;

        x = gridIndex % width;
        y = gridIndex / width;

        return y <= maxY;
    }

    private bool ValidateWalkableGridPosition(int x, int y)
    {
        return GridSetup.Instance.PathGrid.GetGridObject(x, y).IsWalkable();
    }

    private bool ValidateVacantGridPosition(int x, int y)
    {
        return !GridSetup.Instance.OccupationGrid.GetGridObject(x, y).IsOccupied();
    }
}