using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public partial class DeliveringUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (deliveringUnit, entity) in SystemAPI.Query<RefRW<DeliveringUnit>>().WithDisabled<DeliveringUnit>().WithEntityAccess())
        {
            if (!deliveringUnit.ValueRO.IsDisplayingDeliveryItem)
            {
                continue;
            }
            deliveringUnit.ValueRW.IsDisplayingDeliveryItem = false;
            var childLevel1 = EntityManager.GetBuffer<Child>(entity)[0].Value;
            var childLevel2 = EntityManager.GetBuffer<Child>(childLevel1)[0].Value;
            EntityManager.SetComponentEnabled<MaterialMeshInfo>(childLevel2, false);
        }
        foreach (var (deliveringUnit, entity) in SystemAPI.Query<RefRW<DeliveringUnit>>().WithAll<DeliveringUnit>().WithEntityAccess())
        {
            if (deliveringUnit.ValueRO.IsDisplayingDeliveryItem)
            {
                continue;
            }
            deliveringUnit.ValueRW.IsDisplayingDeliveryItem = true;
            var childLevel1 = EntityManager.GetBuffer<Child>(entity)[0].Value;
            var childLevel2 = EntityManager.GetBuffer<Child>(childLevel1)[0].Value;
            EntityManager.SetComponentEnabled<MaterialMeshInfo>(childLevel2, true);
        }
    }
}
