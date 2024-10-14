using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile]
public partial struct DeliveringUnitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (deliveringUnit, entity) in SystemAPI.Query<RefRW<DeliveringUnit>>().WithDisabled<DeliveringUnit>().WithEntityAccess())
        {
            if (deliveringUnit.ValueRO.IsDisplayingDeliveryItem)
            {
                EnableDeliveryGraphic(ref state, deliveringUnit, entity, false);
            }
        }

        foreach (var (deliveringUnit, entity) in SystemAPI.Query<RefRW<DeliveringUnit>>()
                     .WithAll<DeliveringUnit>().WithEntityAccess())
        {
            if (!deliveringUnit.ValueRO.IsDisplayingDeliveryItem)
            {
                EnableDeliveryGraphic(ref state, deliveringUnit, entity, true);
            }
        }
    }

    private static void EnableDeliveryGraphic(ref SystemState state, RefRW<DeliveringUnit> deliveringUnit, Entity entity, bool enable)
    {
        deliveringUnit.ValueRW.IsDisplayingDeliveryItem = enable;
        var child = state.EntityManager.GetBuffer<Child>(entity)[0].Value;
        state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(child, enable);
    }
}