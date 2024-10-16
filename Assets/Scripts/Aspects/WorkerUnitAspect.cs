using Unity.Entities;
using Unity.Mathematics;

public readonly partial struct WorkerUnitAspect : IAspect
{
    public readonly EnabledRefRW<HarvestingUnit> HarvestingUnitEnabled;
    public readonly RefRW<HarvestingUnit> HarvestingUnit;
    public readonly EnabledRefRW<DeliveringUnit> DeliveringUnitEnabled;
    public readonly RefRW<DeliveringUnit> DeliveringUnit;

    public void DisableHarvestAndDelivering(Entity entity)
    {
        HarvestingUnitEnabled.ValueRW = false;
        HarvestingUnit.ValueRW.Target = new int2(-1, -1);
        DeliveringUnitEnabled.ValueRW = false;
    }

    public void EnableHarvestAfterDelivering()
    {
        HarvestingUnitEnabled.ValueRW = true;
        DeliveringUnitEnabled.ValueRW = false;
    }

    public void SetupHarvesting(int2 target)
    {
        HarvestingUnitEnabled.ValueRW = true;
        HarvestingUnit.ValueRW.Target = target;
        DeliveringUnitEnabled.ValueRW = false;
    }

    public void SetupDelivery(int2 dropPoint)
    {
        DeliveringUnitEnabled.ValueRW = true;
        DeliveringUnit.ValueRW.Target = dropPoint;
    }

    // Problem: Not all of these are needed in all contexts. This aspect would be inefficient. 
}