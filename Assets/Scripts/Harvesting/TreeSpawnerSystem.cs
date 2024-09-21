using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
            if (gridPath == null || gridDamageable == null)
            {
                return;
            }

            if (!gridPath.IsWalkable() || gridDamageable.IsDamageable())
            {
                return;
            }

            SpawnTree(gridPath, gridDamageable, mousePosition, treeSpawner);
        }
    }

    private void SpawnTree(GridPath gridPath, GridDamageable gridDamageable, Vector3 mousePosition, TreeSpawner treeSpawner)
    {
        gridPath.SetIsWalkable(false);
        gridDamageable.SetHealth(MaxHealth);

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