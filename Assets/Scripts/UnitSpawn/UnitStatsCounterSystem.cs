using UnitAgency;
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
    }
}