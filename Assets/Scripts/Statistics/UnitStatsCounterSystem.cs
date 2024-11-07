using UnitAgency;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Entities;

namespace Statistics
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(IsDecidingSystem))]
    public partial class UnitStatsCounterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var unitQuery = GetEntityQuery(typeof(UnitAnimationSelection));
            var unitCount = unitQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfUnits(unitCount);

            var isDecidingQuery = GetEntityQuery(typeof(IsDeciding));
            var isDecidingCount = isDecidingQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfDecidingUnits(isDecidingCount);

            var isPathfindingQuery = GetEntityQuery(typeof(PathfindingParams));
            var isPathfindingCount = isPathfindingQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfPathfindingUnits(isPathfindingCount);

            var isSeekingBedQuery = GetEntityQuery(typeof(IsSeekingBed));
            var isSeekingBedCount = isSeekingBedQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfBedSeekingUnits(isSeekingBedCount);

            var isSeekingTreeQuery = GetEntityQuery(typeof(IsSeekingTree));
            var isSeekingTreeCount = isSeekingTreeQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfTreeSeekingUnits(isSeekingTreeCount);

            var isSeekingDropPointQuery = GetEntityQuery(typeof(IsSeekingDropPoint));
            var isSeekingDropPointCount = isSeekingDropPointQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfDropPointSeekingUnits(isSeekingDropPointCount);

            var isSleepingQuery = GetEntityQuery(typeof(IsSleeping));
            var isSleepingCount = isSleepingQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfSleepingUnits(isSleepingCount);

            var isHarvestingQuery = GetEntityQuery(typeof(IsHarvesting));
            var isHarvestingCount = isHarvestingQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfHarvestingUnits(isHarvestingCount);

            var isIdleQuery = GetEntityQuery(typeof(IsIdle));
            var isIdleCount = isIdleQuery.CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIdleUnits(isIdleCount);
        }
    }
}