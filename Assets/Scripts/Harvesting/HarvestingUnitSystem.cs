using Unity.Entities;
using Unity.Mathematics;

public partial class HarvestingUnitSystem : SystemBase
{
    private const int DamagePerSec = -20;

    protected override void OnUpdate()
    {
        foreach (var (harvestingUnit, pathFollow, entity) in SystemAPI.Query<RefRW<HarvestingUnit>, RefRO<PathFollow>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            var unitIsTryingToHarvest = pathFollow.ValueRO.PathIndex < 0;
            if (!unitIsTryingToHarvest)
            {
                continue;
            }

            var targetX = harvestingUnit.ValueRO.Target.x;
            var targetY = harvestingUnit.ValueRO.Target.y;

            var tileHasNoTree = GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).IsWalkable();
            if (tileHasNoTree)
            {
                // Tree was probably destroyed, so please stop chopping it!
                EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                harvestingUnit.ValueRW.Target = new int2(-1, -1);
                continue;
            }

            var gridDamageableObject = GridSetup.Instance.DamageableGrid.GetGridObject(targetX, targetY);
            gridDamageableObject.AddToHealth(DamagePerSec * SystemAPI.Time.DeltaTime);
            if (!gridDamageableObject.IsDamageable())
            {
                // DESTROY TREE:
                GridSetup.Instance.PathGrid.GetGridObject(targetX, targetY).SetIsWalkable(true);
                gridDamageableObject.SetHealth(0);
            }
        }
    }
}