using UnitAgency;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Collections;
using Unity.Entities;

namespace Statistics
{
    [UpdateInGroup(typeof(UnitStateSystemGroup), OrderFirst = true)]
    public partial class UnitStatsCounterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var unitCount = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitAnimationSelection>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfUnits(unitCount);

            var isDecidingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsDeciding>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfDecidingUnits(isDecidingCount);

            var isPathfindingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<PathfindingParams>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfPathfindingUnits(isPathfindingCount);

            var isSeekingBedCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsSeekingBed>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfBedSeekingUnits(isSeekingBedCount);

            var isSeekingTreeCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsSeekingTree>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfTreeSeekingUnits(isSeekingTreeCount);

            var isSeekingDropPointCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsSeekingDropPoint>()
                .Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfDropPointSeekingUnits(isSeekingDropPointCount);

            var isSleepingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsSleeping>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfSleepingUnits(isSleepingCount);

            var isHarvestingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsHarvesting>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfHarvestingUnits(isHarvestingCount);

            var isIdleCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsIdle>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIdleUnits(isIdleCount);
        }
    }
}