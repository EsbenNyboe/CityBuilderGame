using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class UnitSelectionSystem : SystemBase
{
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var material = SelectionAreaManager.Instance.UnitSelectedMaterial;
        var mesh = SelectionAreaManager.Instance.UnitSelectedMesh;

        CameraController.Instance.FollowPosition = Vector3.zero;

        var positionOffset = new float3(0, -0.4f, 0);
        foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
        {
            Graphics.DrawMesh(mesh, localTransform.ValueRO.Position + positionOffset, Quaternion.identity, material, 0);
            CameraController.Instance.FollowPosition = localTransform.ValueRO.Position;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelectedUnits();
        }
    }

    private void DeleteSelectedUnits()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);


        foreach (var (unitSelection, localTransform, entity) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            var unitPosition = localTransform.ValueRO.Position;
            GridHelpers.GetXY(unitPosition, out var x, out var y);
            GridHelpers.SetIsWalkable(ref gridManager, x, y, true);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

            if (GridSetup.Instance.OccupationGrid.GetGridObject(x, y).EntityIsOwner(entity))
            {
                GridSetup.Instance.OccupationGrid.GetGridObject(x, y).SetOccupied(Entity.Null);
            }

            entityCommandBuffer.DestroyEntity(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}