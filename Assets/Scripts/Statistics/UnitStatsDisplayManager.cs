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

        [SerializeField] private bool _showIsSeekingFilledStorage;
        [SerializeField] private bool _showIsEatingMeat;
        [SerializeField] private bool _showIsCookingMeat;
        [SerializeField] private bool _showIsSeekingBonfire;
        [SerializeField] private bool _showIsHoldingSpear;
        [SerializeField] private bool _showIsThrowingSpear;
        [SerializeField] private bool _showIsSeekingDroppedItem;
        [SerializeField] private bool _showIsSeekingConstructable;
        [SerializeField] private bool _showHasLog;
        [SerializeField] private bool _showHasRawMeat;
        [SerializeField] private bool _showHasCookedMeat;

        [SerializeField] private bool _showStoredNothing;
        [SerializeField] private bool _showStoredLog;
        [SerializeField] private bool _showStoredRawMeat;
        [SerializeField] private bool _showStoredCookedMeat;

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

        [SerializeField] private UnitStatsDisplay _isSeekingFilledStorage;
        [SerializeField] private UnitStatsDisplay _isEatingMeat;
        [SerializeField] private UnitStatsDisplay _isCookingMeat;
        [SerializeField] private UnitStatsDisplay _isSeekingBonfire;
        [SerializeField] private UnitStatsDisplay _isHoldingSpear;
        [SerializeField] private UnitStatsDisplay _isThrowingSpear;
        [SerializeField] private UnitStatsDisplay _isSeekingDroppedItem;
        [SerializeField] private UnitStatsDisplay _isSeekingConstructable;
        [SerializeField] private UnitStatsDisplay _hasLog;
        [SerializeField] private UnitStatsDisplay _hasRawMeat;
        [SerializeField] private UnitStatsDisplay _hasCookedMeat;

        [SerializeField] private UnitStatsDisplay _storedNothing;
        [SerializeField] private UnitStatsDisplay _storedLog;
        [SerializeField] private UnitStatsDisplay _storedRawMeat;
        [SerializeField] private UnitStatsDisplay _storedCookedMeat;

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

            _isSeekingFilledStorage.gameObject.SetActive(_showIsSeekingFilledStorage);
            _isEatingMeat.gameObject.SetActive(_showIsEatingMeat);
            _isCookingMeat.gameObject.SetActive(_showIsCookingMeat);
            _isSeekingBonfire.gameObject.SetActive(_showIsSeekingBonfire);
            _isHoldingSpear.gameObject.SetActive(_showIsHoldingSpear);
            _isThrowingSpear.gameObject.SetActive(_showIsThrowingSpear);
            _isSeekingDroppedItem.gameObject.SetActive(_showIsSeekingDroppedItem);
            _isSeekingConstructable.gameObject.SetActive(_showIsSeekingConstructable);

            _hasLog.gameObject.SetActive(_showHasLog);
            _hasRawMeat.gameObject.SetActive(_showHasRawMeat);
            _hasCookedMeat.gameObject.SetActive(_showHasCookedMeat);

            _storedNothing.gameObject.SetActive(_showStoredNothing);
            _storedLog.gameObject.SetActive(_showStoredLog);
            _storedRawMeat.gameObject.SetActive(_showStoredRawMeat);
            _storedCookedMeat.gameObject.SetActive(_showStoredCookedMeat);
        }

        public void SetNumberOfUnits(int count)
        {
            _numberOfUnitsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfDecidingUnits(int count)
        {
            _numberOfDecisionsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfPathfindingUnits(int count)
        {
            _numberOfPathfindingsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfPathInvalidations(int count)
        {
            _numberOfPathInvalidationsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfConversationEvents(int count)
        {
            _numberOfConversationsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfSocialEvent(int count)
        {
            _numberOfSocialEventDisplay.SetStatsValue(count);
        }

        public void SetNumberOfSocialEventWithVictim(int count)
        {
            _numberOfSocialEventWithVictimDisplay.SetStatsValue(count);
        }

        public void SetNumberOfPositiveSocialEffects(int count)
        {
            _numberOfPositiveSocialEffectsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfNegativeSocialEffects(int count)
        {
            _numberOfNegativeSocialEffectsDisplay.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingBed(int count)
        {
            _isSeekingBed.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingTree(int count)
        {
            _isSeekingTree.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingRoomyStorage(int count)
        {
            _isSeekingRoomyStorage.SetStatsValue(count);
        }

        public void SetNumberOfIsSleeping(int count)
        {
            _isSleeping.SetStatsValue(count);
        }

        public void SetNumberOfIsHarvesting(int count)
        {
            _isHarvesting.SetStatsValue(count);
        }

        public void SetNumberOfIsIdle(int count)
        {
            _isIdle.SetStatsValue(count);
        }

        public void SetNumberOfIsTalkative(int count)
        {
            _isTalkative.SetStatsValue(count);
        }

        public void SetNumberOfIsTalking(int count)
        {
            _isTalking.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingTalkingPartner(int count)
        {
            _isSeekingTalkingPartner.SetStatsValue(count);
        }

        public void SetNumberOfIsAttemptingMurder(int count)
        {
            _isAttemptingMurder.SetStatsValue(count);
        }

        public void SetNumberOfIsMurdering(int count)
        {
            _isMurdering.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingFilledStorage(int count)
        {
            _isSeekingFilledStorage.SetStatsValue(count);
        }

        public void SetNumberOfIsEatingMeat(int count)
        {
            _isEatingMeat.SetStatsValue(count);
        }

        public void SetNumberOfIsCookingMeat(int count)
        {
            _isCookingMeat.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingBonfire(int count)
        {
            _isSeekingBonfire.SetStatsValue(count);
        }

        public void SetNumberOfIsHoldingSpear(int count)
        {
            _isHoldingSpear.SetStatsValue(count);
        }

        public void SetNumberOfIsThrowingSpear(int count)
        {
            _isThrowingSpear.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingDroppedItem(int count)
        {
            _isSeekingDroppedItem.SetStatsValue(count);
        }

        public void SetNumberOfIsSeekingConstructable(int count)
        {
            _isSeekingConstructable.SetStatsValue(count);
        }

        public void SetNumberOfHasLog(int count)
        {
            _hasLog.SetStatsValue(count);
        }

        public void SetNumberOfHasRawMeat(int count)
        {
            _hasRawMeat.SetStatsValue(count);
        }

        public void SetNumberOfHasCookedMeat(int count)
        {
            _hasCookedMeat.SetStatsValue(count);
        }

        public void SetNumberOfStoredNothing(int count)
        {
            _storedNothing.SetStatsValue(count);
        }

        public void SetNumberOfStoredLog(int count)
        {
            _storedLog.SetStatsValue(count);
        }

        public void SetNumberOfStoredRawMeat(int count)
        {
            _storedRawMeat.SetStatsValue(count);
        }

        public void SetNumberOfStoredCookedMeat(int count)
        {
            _storedCookedMeat.SetStatsValue(count);
        }
    }
}