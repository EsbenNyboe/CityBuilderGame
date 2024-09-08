using Unity.Entities;
using UnityEngine;

public class HarvestingUnitAuthoring : MonoBehaviour
{
    public class Baker : Baker<HarvestingUnitAuthoring>
    {
        public override void Bake(HarvestingUnitAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new HarvestingUnit());
            SetComponentEnabled<HarvestingUnit>(entity, false);
        }
    }
}

public struct HarvestingUnit : IComponentData, IEnableableComponent
{
}