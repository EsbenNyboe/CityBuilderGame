using PathInvalidation;
using Rendering.SpriteTransformNS;
using UnitAgency.Data;
using UnitBehaviours;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Idle;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using UnitState.SocialState;
using Unity.Entities;

namespace Statistics
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UnitStatsCounterSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private EntityQuery _isDecidingQuery;
        private EntityQuery _isPathfindingQuery;
        private EntityQuery _conversationEventQuery;
        private EntityQuery _socialEventQuery;
        private EntityQuery _socialEventWithVictimQuery;
        private EntityQuery _isSeekingBedQuery;
        private EntityQuery _isSeekingTreeQuery;
        private EntityQuery _isSeekingDropPointQuery;
        private EntityQuery _isSleepingQuery;
        private EntityQuery _isHarvestingQuery;
        private EntityQuery _isIdleQuery;
        private EntityQuery _isTalkativeQuery;
        private EntityQuery _isTalkingQuery;
        private EntityQuery _isSeekingTalkingPartnerQuery;
        private EntityQuery _isAttemptingMurderQuery;
        private EntityQuery _isMurderingQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();

            _villagerQuery = state.GetEntityQuery(typeof(Villager));
            _isDecidingQuery = state.GetEntityQuery(typeof(IsDeciding), ComponentType.Exclude<AttackAnimation>());
            _isPathfindingQuery = state.GetEntityQuery(typeof(Pathfinding));
            _conversationEventQuery = state.GetEntityQuery(typeof(ConversationEvent));
            _socialEventQuery = state.GetEntityQuery(typeof(SocialEvent));
            _socialEventWithVictimQuery = state.GetEntityQuery(typeof(SocialEventWithVictim));
            _isSeekingBedQuery = state.GetEntityQuery(typeof(IsSeekingBed));
            _isSeekingTreeQuery = state.GetEntityQuery(typeof(IsSeekingTree));
            _isSeekingDropPointQuery = state.GetEntityQuery(typeof(IsSeekingDropPoint));
            _isSleepingQuery = state.GetEntityQuery(typeof(IsSleeping));
            _isHarvestingQuery = state.GetEntityQuery(typeof(IsHarvesting));
            _isIdleQuery = state.GetEntityQuery(typeof(IsIdle));
            _isTalkativeQuery = state.GetEntityQuery(typeof(IsTalkative));
            _isTalkingQuery = state.GetEntityQuery(typeof(IsTalking));
            _isSeekingTalkingPartnerQuery = state.GetEntityQuery(typeof(IsSeekingTalkingPartner));
            _isAttemptingMurderQuery = state.GetEntityQuery(typeof(IsAttemptingMurder));
            _isMurderingQuery = state.GetEntityQuery(typeof(IsMurdering));
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var instance = UnitStatsDisplayManager.Instance;
            instance.SetNumberOfUnits(_villagerQuery.CalculateEntityCount());
            instance.SetNumberOfDecidingUnits(_isDecidingQuery.CalculateEntityCount());
            instance.SetNumberOfPathfindingUnits(_isPathfindingQuery.CalculateEntityCount());

            foreach (var (pathInvalidationDebugEvent, entity) in SystemAPI.Query<RefRO<PathInvalidationDebugEvent>>().WithEntityAccess())
            {
                instance.SetNumberOfPathInvalidations(pathInvalidationDebugEvent.ValueRO.Count);
                ecb.DestroyEntity(entity);
            }

            instance.SetNumberOfConversationEvents(_conversationEventQuery.CalculateEntityCount());
            instance.SetNumberOfSocialEvent(_socialEventQuery.CalculateEntityCount());
            instance.SetNumberOfSocialEventWithVictim(_socialEventWithVictimQuery.CalculateEntityCount());
            instance.SetNumberOfBedSeekingUnits(_isSeekingBedQuery.CalculateEntityCount());
            instance.SetNumberOfTreeSeekingUnits(_isSeekingTreeQuery.CalculateEntityCount());
            instance.SetNumberOfDropPointSeekingUnits(_isSeekingDropPointQuery.CalculateEntityCount());
            instance.SetNumberOfSleepingUnits(_isSleepingQuery.CalculateEntityCount());
            instance.SetNumberOfHarvestingUnits(_isHarvestingQuery.CalculateEntityCount());
            instance.SetNumberOfIdleUnits(_isIdleQuery.CalculateEntityCount());
            instance.SetNumberOfIsTalkative(_isTalkativeQuery.CalculateEntityCount());
            instance.SetNumberOfIsTalking(_isTalkingQuery.CalculateEntityCount());
            instance.SetNumberOfIsSeekingTalkingPartner(_isSeekingTalkingPartnerQuery.CalculateEntityCount());
            instance.SetNumberOfIsAttemptingMurder(_isAttemptingMurderQuery.CalculateEntityCount());
            instance.SetNumberOfIsMurdering(_isMurderingQuery.CalculateEntityCount());
        }
    }
}