using UnitBehaviours.AutonomousHarvesting;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public struct DeliveringUnitTag : IComponentData
{
}

[BurstCompile]
public partial struct DeliveringUnitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // TODO: Remove this, and integrate resource-graphic in unit-animation-sheet (or something else, maybe)
        foreach (var (child, entity) in SystemAPI.Query<DynamicBuffer<Child>>().WithNone<IsSeekingDropPoint>().WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(child[0].Value, false);
        }

        foreach (var (child, entity) in SystemAPI.Query<DynamicBuffer<Child>>()
                     .WithAll<IsSeekingDropPoint>().WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(child[0].Value, true);
        }
    }
}