using CodeMonkey.Utils;
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

            SpawnTreeWithoutEntity(gridPath, gridDamageable);
        }
    }

    private void SpawnTreeWithoutEntity(GridPath gridPath, GridDamageable gridDamageable)
    {
        gridPath.SetIsWalkable(false);
        gridDamageable.SetHealth(MaxHealth);
    }
}