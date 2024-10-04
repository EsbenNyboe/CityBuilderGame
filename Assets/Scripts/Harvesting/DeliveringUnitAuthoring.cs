using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class DeliveringUnitAuthoring : MonoBehaviour
{
    public class Baker : Baker<DeliveringUnitAuthoring>
    {
        public override void Bake(DeliveringUnitAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new DeliveringUnit
            {
                Target = new int2(-1, -1),
                IsDisplayingDeliveryItem = true
            });
            SetComponentEnabled<DeliveringUnit>(entity, false);
        }
    }
}

public struct DeliveringUnit : IComponentData, IEnableableComponent
{
    public int2 Target;
    public bool IsDisplayingDeliveryItem;
}