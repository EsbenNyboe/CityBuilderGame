using UnitBehaviours.AutonomousHarvesting;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(PreRenderingSystemGroup))]
[BurstCompile]
public partial struct InventoryItemVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // TODO: Remove this, and integrate resource-graphic in unit-animation-sheet (or something else, maybe)
        foreach (var (child, entity) in SystemAPI.Query<DynamicBuffer<Child>>().WithAll<Inventory>()
                     .WithNone<IsSeekingDropPoint>().WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(child[0].Value, false);
        }

        foreach (var (child, entity) in SystemAPI.Query<DynamicBuffer<Child>>().WithAll<Inventory>()
                     .WithAll<IsSeekingDropPoint>().WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(child[0].Value, true);
        }
    }
}