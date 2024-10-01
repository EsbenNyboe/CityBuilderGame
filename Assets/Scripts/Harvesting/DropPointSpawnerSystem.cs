using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class DropPointSpawnerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DropPointSpawner>();
    }

    protected override void OnUpdate()
    {
        var dropPointSpawner = SystemAPI.GetSingleton<DropPointSpawner>();
        var pathGrid = GridSetup.Instance.PathGrid;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathGrid.GetCellSize() * .5f;

        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
        {
            var gridNode = pathGrid.GetGridObject(mousePosition);
            if (gridNode == null || !gridNode.IsWalkable())
            {
                return;
            }

            gridNode.SetIsWalkable(false);

            GridSetup.Instance.PathGrid.GetXY(mousePosition, out var x, out var y);

            var spawnedEntity = EntityManager.Instantiate(dropPointSpawner.ObjectToSpawn);
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
}