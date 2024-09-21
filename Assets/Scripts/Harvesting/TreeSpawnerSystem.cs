using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class TreeSpawnerSystem : SystemBase
{
    private static bool _shouldSpawnTreesOnMouseDown;

    protected override void OnCreate()
    {
        RequireForUpdate<TreeSpawner>();
    }

    protected override void OnUpdate()
    {
        var treeSpawner = SystemAPI.GetSingleton<TreeSpawner>();
        var pathfindingGrid = GridSetup.Instance.PathGrid;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f;

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
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
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode == null)
            {
                return;
            }

            if (!gridNode.IsWalkable())
            {
                return;
            }

            SpawnTree(gridNode, mousePosition, treeSpawner);
        }
    }

    private void SpawnTree(GridPath gridPath, Vector3 mousePosition, TreeSpawner treeSpawner)
    {
        gridPath.SetIsWalkable(false);

        GridSetup.Instance.PathGrid.GetXY(mousePosition, out var x, out var y);

        var spawnedEntity = EntityManager.Instantiate(treeSpawner.ObjectToSpawn);
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
    }
}