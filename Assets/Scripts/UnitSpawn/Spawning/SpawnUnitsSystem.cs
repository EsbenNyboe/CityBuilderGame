using Debugging;
using Grid;
using SystemGroups;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitSpawn.Spawning
{
    [UpdateInGroup(typeof(LifetimeSystemGroup), OrderFirst = true)]
    public partial class SpawnUnitsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<SpawnUnitsConfig>();
        }

        protected override void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            var spawnUnitsConfig = SystemAPI.GetSingleton<SpawnUnitsConfig>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            var gridIndex = 0;
            for (var i = 0; i < spawnUnitsConfig.AmountToSpawn; i++)
            {
                if (!GetNextValidGridPosition(gridManager, ref gridIndex, out var x, out var y))
                {
                    return;
                }

                var spawnedEntity = EntityManager.Instantiate(spawnUnitsConfig.ObjectToSpawn);
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

                gridManager.SetOccupant(x, y, spawnedEntity);
                SystemAPI.SetSingleton(gridManager);
            }
        }

        private bool GetNextValidGridPosition(GridManager gridManager, ref int gridIndex, out int x, out int y)
        {
            var maxAttempts = 20000;
            var currentAttempt = 0;
            x = 0;
            y = 0;

            while (currentAttempt < maxAttempts)
            {
                currentAttempt++;

                if (!GetNextGridPosition(gridManager, gridIndex, out x, out y))
                {
                    DebugHelper.Log("No valid grid position found: Outside range");
                    return false;
                }

                if (ValidateWalkableGridPosition(gridManager, gridIndex) && ValidateVacantGridPosition(gridManager, x, y))
                {
                    return true;
                }

                gridIndex++;
            }

            DebugHelper.Log("No valid grid position found: Not walkable");
            return false;
        }

        private bool GetNextGridPosition(GridManager gridManager, int gridIndex, out int x, out int y)
        {
            if (gridIndex >= gridManager.WalkableGrid.Length)
            {
                x = -1;
                y = -1;
                return false;
            }

            gridManager.GetXY(gridIndex, out x, out y);
            return true;
        }

        private bool ValidateWalkableGridPosition(GridManager gridManager, int gridIndex)
        {
            return gridManager.WalkableGrid[gridIndex].IsWalkable;
        }

        private bool ValidateVacantGridPosition(GridManager gridManager, int x, int y)
        {
            return !gridManager.IsOccupied(x, y);
        }
    }
}