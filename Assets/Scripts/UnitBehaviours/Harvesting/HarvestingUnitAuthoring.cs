using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class HarvestingUnitAuthoring : MonoBehaviour
{
    public class Baker : Baker<HarvestingUnitAuthoring>
    {
        public override void Bake(HarvestingUnitAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new HarvestingUnit
            {
                Target = new int2(-1, -1)
            });
        }
    }
}

public struct HarvestingUnit : IComponentData
{
    public int2 Target;
    public float TimeUntilNextChop;
}