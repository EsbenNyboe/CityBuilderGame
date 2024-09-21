using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class HarvestingUnitAuthoring : MonoBehaviour
{
    public class Baker : Baker<HarvestingUnitAuthoring>
    {
        public override void Bake(HarvestingUnitAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new HarvestingUnit
            {
                IsHarvesting = false,
                Target = new int2(-1,-1)
            });
            SetComponentEnabled<HarvestingUnit>(entity, false);
        }
    }
}

public struct HarvestingUnit : IComponentData, IEnableableComponent
{
    public bool IsHarvesting;
    public int2 Target;
}