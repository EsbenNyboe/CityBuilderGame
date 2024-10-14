using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class UnitSelectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var material = SelectionAreaManager.Instance.UnitSelectedMaterial;
        var mesh = SelectionAreaManager.Instance.UnitSelectedMesh;

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

        foreach (var (unitSelection, localTransform, entity) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            var unitPosition = localTransform.ValueRO.Position;
            GridSetup.Instance.PathGrid.GetXY(unitPosition, out var x, out var y);
            GridSetup.Instance.PathGrid.GetGridObject(x, y).SetIsWalkable(true);
            if (GridSetup.Instance.OccupationGrid.GetGridObject(x, y).EntityIsOwner(entity))
            {
                GridSetup.Instance.OccupationGrid.GetGridObject(x, y).SetOccupied(Entity.Null);
            }

            entityCommandBuffer.DestroyEntity(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}