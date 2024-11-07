using UnitAgency;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Entities;

[UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(IsDecidingSystem))]
public partial class UnitStatsCounterSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var unitQuery = GetEntityQuery(typeof(UnitAnimationSelection));
        var unitCount = unitQuery.CalculateEntityCount();
        UnitStatsDisplay.Instance.SetNumberOfUnits(unitCount);

        var isDecidingQuery = GetEntityQuery(typeof(IsDeciding));
        var isDecidingCount = isDecidingQuery.CalculateEntityCount();
        UnitStatsDisplay.Instance.SetNumberOfDecidingUnits(isDecidingCount);

        var isSeekingBedQuery = GetEntityQuery(typeof(IsSeekingBed));
        var isSeekingBedCount = isSeekingBedQuery.CalculateEntityCount();
        UnitStatsDisplay.Instance.SetNumberOfBedSeekingUnits(isSeekingBedCount);

        var isSeekingTreeQuery = GetEntityQuery(typeof(IsSeekingTree));
        var isSeekingTreeCount = isSeekingTreeQuery.CalculateEntityCount();
        UnitStatsDisplay.Instance.SetNumberOfTreeSeekingUnits(isSeekingTreeCount);
    }
}