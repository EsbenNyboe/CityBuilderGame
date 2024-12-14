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

        [SerializeField] private bool _showNumberOfBedSeekers;
        [SerializeField] private bool _showNumberOfTreeSeekers;
        [SerializeField] private bool _showNumberOfDropPointsSeekers;
        [SerializeField] private bool _showNumberOfSleepers;
        [SerializeField] private bool _showNumberOfHarvesters;
        [SerializeField] private bool _showNumberOfIdle;
        [SerializeField] private bool _showNumberOfTalkative;
        [SerializeField] private bool _showNumberOfTalking;
        [SerializeField] private bool _showNumberOfIsSeekingTalkingPartner;
        [SerializeField] private bool _showNumberOfIsAttemptingMurder;
        [SerializeField] private bool _showNumberOfIsMurdering;

        [SerializeField] private UnitStatsDisplay _numberOfUnitsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDecisionsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPathfindingsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPathInvalidationsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfConversationsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSocialEventDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSocialEventWithVictimDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPositiveSocialEffectsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfNegativeSocialEffectsDisplay;


        [SerializeField] private UnitStatsDisplay _numberOfBedSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfTreeSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDropPointSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSleepersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfHarvestersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfIdleDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfTalkativeDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfTalkingDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfIsSeekingTalkingPartnerDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfIsAttemptingMurderDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfIsMurderingDisplay;


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

            _numberOfBedSeekersDisplay.gameObject.SetActive(_showNumberOfBedSeekers);
            _numberOfTreeSeekersDisplay.gameObject.SetActive(_showNumberOfTreeSeekers);
            _numberOfDropPointSeekersDisplay.gameObject.SetActive(_showNumberOfDropPointsSeekers);
            _numberOfSleepersDisplay.gameObject.SetActive(_showNumberOfSleepers);
            _numberOfHarvestersDisplay.gameObject.SetActive(_showNumberOfHarvesters);
            _numberOfIdleDisplay.gameObject.SetActive(_showNumberOfIdle);
            _numberOfTalkativeDisplay.gameObject.SetActive(_showNumberOfTalkative);
            _numberOfTalkingDisplay.gameObject.SetActive(_showNumberOfTalking);
            _numberOfIsSeekingTalkingPartnerDisplay.gameObject.SetActive(_showNumberOfIsSeekingTalkingPartner);
            _numberOfIsAttemptingMurderDisplay.gameObject.SetActive(_showNumberOfIsAttemptingMurder);
            _numberOfIsMurderingDisplay.gameObject.SetActive(_showNumberOfIsMurdering);
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

        public void SetNumberOfBedSeekingUnits(int isSeekingBedCount)
        {
            _numberOfBedSeekersDisplay.SetStatsValue(isSeekingBedCount);
        }

        public void SetNumberOfTreeSeekingUnits(int isSeekingTreeCount)
        {
            _numberOfTreeSeekersDisplay.SetStatsValue(isSeekingTreeCount);
        }

        public void SetNumberOfDropPointSeekingUnits(int isSeekingDropPointCount)
        {
            _numberOfDropPointSeekersDisplay.SetStatsValue(isSeekingDropPointCount);
        }

        public void SetNumberOfSleepingUnits(int isSleepingCount)
        {
            _numberOfSleepersDisplay.SetStatsValue(isSleepingCount);
        }

        public void SetNumberOfHarvestingUnits(int isHarvestingCount)
        {
            _numberOfHarvestersDisplay.SetStatsValue(isHarvestingCount);
        }

        public void SetNumberOfIdleUnits(int isIdleCount)
        {
            _numberOfIdleDisplay.SetStatsValue(isIdleCount);
        }

        public void SetNumberOfIsTalkative(int isTalkativeCount)
        {
            _numberOfTalkativeDisplay.SetStatsValue(isTalkativeCount);
        }

        public void SetNumberOfIsTalking(int isTalkingCount)
        {
            _numberOfTalkingDisplay.SetStatsValue(isTalkingCount);
        }

        public void SetNumberOfIsSeekingTalkingPartner(int isSeekingTalkingPartnerCount)
        {
            _numberOfIsSeekingTalkingPartnerDisplay.SetStatsValue(isSeekingTalkingPartnerCount);
        }

        public void SetNumberOfIsAttemptingMurder(int isAttemptingMurderCount)
        {
            _numberOfIsAttemptingMurderDisplay.SetStatsValue(isAttemptingMurderCount);
        }

        public void SetNumberOfIsMurdering(int isMurderingCount)
        {
            _numberOfIsMurderingDisplay.SetStatsValue(isMurderingCount);
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
    }
}