using Statistics.StatsDisplays;
using UnityEngine;

namespace Statistics
{
    public class UnitStatsDisplayManager : MonoBehaviour
    {
        public static UnitStatsDisplayManager Instance;
        [SerializeField] private bool _showNumberOfUnits;
        [SerializeField] private bool _showNumberOfDecisions;
        [SerializeField] private bool _showNumberOfPathfindings;
        [SerializeField] private bool _showNumberOfPathInvalidations;
        [SerializeField] private bool _showNumberOfConversations;
        [SerializeField] private bool _showNumberOfSocialEvent;
        [SerializeField] private bool _showNumberOfSocialEventWithVictim;
        [SerializeField] private bool _showNumberOfPositiveSocialEffects;
        [SerializeField] private bool _showNumberOfNegativeSocialEffects;

        [SerializeField] private bool _showIsSeekingBed;
        [SerializeField] private bool _showIsSeekingTree;
        [SerializeField] private bool _showIsSeekingRoomyStorage;
        [SerializeField] private bool _showIsSleeping;
        [SerializeField] private bool _showIsHarvesting;
        [SerializeField] private bool _showIsIdle;
        [SerializeField] private bool _showIsTalkative;
        [SerializeField] private bool _showIsTalking;
        [SerializeField] private bool _showIsSeekingTalkingPartner;
        [SerializeField] private bool _showIsAttemptingMurder;
        [SerializeField] private bool _showIsMurdering;

        [SerializeField] private UnitStatsDisplay _numberOfUnitsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDecisionsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPathfindingsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPathInvalidationsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfConversationsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSocialEventDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSocialEventWithVictimDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPositiveSocialEffectsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfNegativeSocialEffectsDisplay;

        [SerializeField] private UnitStatsDisplay _isSeekingBed;
        [SerializeField] private UnitStatsDisplay _isSeekingTree;
        [SerializeField] private UnitStatsDisplay _isSeekingRoomyStorage;
        [SerializeField] private UnitStatsDisplay _isSleeping;
        [SerializeField] private UnitStatsDisplay _isHarvesting;
        [SerializeField] private UnitStatsDisplay _isIdle;
        [SerializeField] private UnitStatsDisplay _isTalkative;
        [SerializeField] private UnitStatsDisplay _isTalking;
        [SerializeField] private UnitStatsDisplay _isSeekingTalkingPartner;
        [SerializeField] private UnitStatsDisplay _isAttemptingMurder;
        [SerializeField] private UnitStatsDisplay _isMurdering;


        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _numberOfUnitsDisplay.gameObject.SetActive(_showNumberOfUnits);
            _numberOfDecisionsDisplay.gameObject.SetActive(_showNumberOfDecisions);
            _numberOfPathfindingsDisplay.gameObject.SetActive(_showNumberOfPathfindings);
            _numberOfPathInvalidationsDisplay.gameObject.SetActive(_showNumberOfPathInvalidations);
            _numberOfConversationsDisplay.gameObject.SetActive(_showNumberOfConversations);
            _numberOfSocialEventDisplay.gameObject.SetActive(_showNumberOfSocialEvent);
            _numberOfSocialEventWithVictimDisplay.gameObject.SetActive(_showNumberOfSocialEventWithVictim);
            _numberOfPositiveSocialEffectsDisplay.gameObject.SetActive(_showNumberOfPositiveSocialEffects);
            _numberOfNegativeSocialEffectsDisplay.gameObject.SetActive(_showNumberOfNegativeSocialEffects);

            _isSeekingBed.gameObject.SetActive(_showIsSeekingBed);
            _isSeekingTree.gameObject.SetActive(_showIsSeekingTree);
            _isSeekingRoomyStorage.gameObject.SetActive(_showIsSeekingRoomyStorage);
            _isSleeping.gameObject.SetActive(_showIsSleeping);
            _isHarvesting.gameObject.SetActive(_showIsHarvesting);
            _isIdle.gameObject.SetActive(_showIsIdle);
            _isTalkative.gameObject.SetActive(_showIsTalkative);
            _isTalking.gameObject.SetActive(_showIsTalking);
            _isSeekingTalkingPartner.gameObject.SetActive(_showIsSeekingTalkingPartner);
            _isAttemptingMurder.gameObject.SetActive(_showIsAttemptingMurder);
            _isMurdering.gameObject.SetActive(_showIsMurdering);
        }

        public void SetNumberOfUnits(int numberOfUnits)
        {
            _numberOfUnitsDisplay.SetStatsValue(numberOfUnits);
        }

        public void SetNumberOfDecidingUnits(int isDecidingCount)
        {
            _numberOfDecisionsDisplay.SetStatsValue(isDecidingCount);
        }

        public void SetNumberOfPathfindingUnits(int isPathfindingCount)
        {
            _numberOfPathfindingsDisplay.SetStatsValue(isPathfindingCount);
        }

        public void SetNumberOfPathInvalidations(int count)
        {
            _numberOfPathInvalidationsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfConversationEvents(int conversationEventCount)
        {
            _numberOfConversationsDisplay.SetStatsValue(conversationEventCount);
        }

        public void SetNumberOfSocialEvent(int socialEventCount)
        {
            _numberOfSocialEventDisplay.SetStatsValue(socialEventCount);
        }

        public void SetNumberOfSocialEventWithVictim(int socialEventWithVictimCount)
        {
            _numberOfSocialEventWithVictimDisplay.SetStatsValue(socialEventWithVictimCount);
        }

        public void SetNumberOfPositiveSocialEffects(int count)
        {
            _numberOfPositiveSocialEffectsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfNegativeSocialEffects(int count)
        {
            _numberOfNegativeSocialEffectsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingBed(int isSeekingBedCount)
        {
            _isSeekingBed.SetStatsValue(isSeekingBedCount);
        }

        public void SetNumberOfIsSeekingTree(int isSeekingTreeCount)
        {
            _isSeekingTree.SetStatsValue(isSeekingTreeCount);
        }

        public void SetNumberOfIsSeekingRoomyStorage(int isSeekingRoomyStorageCount)
        {
            _isSeekingRoomyStorage.SetStatsValue(isSeekingRoomyStorageCount);
        }

        public void SetNumberOfIsSleeping(int isSleepingCount)
        {
            _isSleeping.SetStatsValue(isSleepingCount);
        }

        public void SetNumberOfIsHarvesting(int isHarvestingCount)
        {
            _isHarvesting.SetStatsValue(isHarvestingCount);
        }

        public void SetNumberOfIsIdle(int isIdleCount)
        {
            _isIdle.SetStatsValue(isIdleCount);
        }

        public void SetNumberOfIsTalkative(int isTalkativeCount)
        {
            _isTalkative.SetStatsValue(isTalkativeCount);
        }

        public void SetNumberOfIsTalking(int isTalkingCount)
        {
            _isTalking.SetStatsValue(isTalkingCount);
        }

        public void SetNumberOfIsSeekingTalkingPartner(int isSeekingTalkingPartnerCount)
        {
            _isSeekingTalkingPartner.SetStatsValue(isSeekingTalkingPartnerCount);
        }

        public void SetNumberOfIsAttemptingMurder(int isAttemptingMurderCount)
        {
            _isAttemptingMurder.SetStatsValue(isAttemptingMurderCount);
        }

        public void SetNumberOfIsMurdering(int isMurderingCount)
        {
            _isMurdering.SetStatsValue(isMurderingCount);
        }
    }
}