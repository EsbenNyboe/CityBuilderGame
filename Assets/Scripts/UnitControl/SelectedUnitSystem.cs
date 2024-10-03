using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class SelectedUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var material = SelectionAreaManager.Instance.UnitSelectedMaterial;
        var mesh = SelectionAreaManager.Instance.UnitSelectedMesh;

        var positionOffset = new float3(0, -0.4f, 0);
        foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
        {
            Graphics.DrawMesh(mesh, localTransform.ValueRO.Position + positionOffset, Quaternion.identity, material, 0);
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelectedUnits();
        }
    }

    private void DeleteSelectedUnits()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (unitSelection, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
        {
            entityCommandBuffer.DestroyEntity(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}