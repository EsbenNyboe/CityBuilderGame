using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class DropPointSpawnerSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        RequireForUpdate<DropPointSpawner>();
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var dropPointSpawner = SystemAPI.GetSingleton<DropPointSpawner>();
        var cellSize = 1f;
        var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * cellSize * .5f;

        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
        {
            GridHelpers.GetXY(mousePosition, out var x, out var y);

            if (!GridHelpers.IsPositionInsideGrid(gridManager, x, y))
            {
                return;
            }

            var gridIndex = GridHelpers.GetIndex(gridManager, x, y);
            if (!GridHelpers.IsWalkable(gridManager, gridIndex))
            {
                return;
            }

            GridHelpers.SetIsWalkable(ref gridManager, gridIndex, false);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

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