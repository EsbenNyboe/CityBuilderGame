using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class BedSpawnerSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        RequireForUpdate<BedSpawner>();
        _gridManagerSystemHandle = EntityManager.World.GetExistingSystem(typeof(GridManagerSystem));
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var mousePos = UtilsClass.GetMouseWorldPosition();
            GridHelpers.GetXY(mousePos, out var x, out var y);
            gridManager.ValidateGridPosition(ref x, ref y);
            if (gridManager.IsWalkable(x, y))
            {
                foreach (var bedSpawner in SystemAPI.Query<RefRO<BedSpawner>>())
                {
                    var entity = EntityManager.Instantiate(bedSpawner.ValueRO.Prefab);
                    SystemAPI.SetComponent(entity, new LocalTransform
                    {
                        Position = new float3(x, y, 0),
                        Scale = 1,
                        Rotation = quaternion.identity
                    });
                }
            }
        }
    }
}