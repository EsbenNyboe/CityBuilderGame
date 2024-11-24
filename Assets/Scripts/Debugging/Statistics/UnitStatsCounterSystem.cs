using UnitAgency;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
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

            var isDecidingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsDeciding>()
                .WithNone<AttackAnimation>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfDecidingUnits(isDecidingCount);

            var isPathfindingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<Pathfinding>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfPathfindingUnits(isPathfindingCount);

            var conversationEventCount = new EntityQueryBuilder(Allocator.Temp).WithAll<ConversationEvent>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfConversationEvents(conversationEventCount);

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

            var isTalkativeCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsTalkative>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIsTalkative(isTalkativeCount);

            var isTalkingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsTalking>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIsTalking(isTalkingCount);

            var isSeekingTalkingPartnerCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsSeekingTalkingPartner>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIsSeekingTalkingPartner(isSeekingTalkingPartnerCount);

            var isAttemptingMurderCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsAttemptingMurder>()
                .Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIsAttemptingMurder(isAttemptingMurderCount);

            var isMurderingCount = new EntityQueryBuilder(Allocator.Temp).WithAll<IsMurdering>().Build(this)
                .CalculateEntityCount();
            UnitStatsDisplayManager.Instance.SetNumberOfIsMurdering(isMurderingCount);
        }
    }
}