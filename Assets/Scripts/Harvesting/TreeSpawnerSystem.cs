using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class TreeSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<TreeSpawner>();
    }

    protected override void OnUpdate()
    {
        var treeSpawner = SystemAPI.GetSingleton<TreeSpawner>();
        var shouldSpawnTreesOnMouseDown = false;
        var pathfindingGrid = PathfindingGridSetup.Instance.pathfindingGrid;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f;

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode == null)
            {
                return;
            }

            UpdateGridCell(gridNode, mousePosition, treeSpawner);

            shouldSpawnTreesOnMouseDown = gridNode.IsWalkable();
        }

        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode == null)
            {
                return;
            }

            if (gridNode.IsWalkable() == shouldSpawnTreesOnMouseDown)
            {
                return;
            }

            UpdateGridCell(gridNode, mousePosition, treeSpawner);
        }
    }

    private void UpdateGridCell(GridNode gridNode, Vector3 mousePosition, TreeSpawner treeSpawner)
    {
        gridNode.SetIsWalkable(!gridNode.IsWalkable());

        PathfindingGridSetup.Instance.pathfindingGrid.GetXY(mousePosition, out var x, out var y);

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