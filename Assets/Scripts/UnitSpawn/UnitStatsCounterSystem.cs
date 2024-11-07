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
    }
}