using UnityEngine;

namespace Statistics
{
    public class UnitStatsDisplayManager : MonoBehaviour
    {
        public static UnitStatsDisplayManager Instance;
        [SerializeField] private bool _showNumberOfUnits;
        [SerializeField] private bool _showNumberOfDecisions;
        [SerializeField] private bool _showNumberOfPathfindings;
        [SerializeField] private bool _showNumberOfBedSeekers;
        [SerializeField] private bool _showNumberOfTreeSeekers;
        [SerializeField] private bool _showNumberOfDropPointsSeekers;
        [SerializeField] private bool _showNumberOfSleepers;
        [SerializeField] private bool _showNumberOfHarvesters;
        [SerializeField] private bool _showNumberOfIdle;

        [SerializeField] private UnitStatsDisplay _numberOfUnitsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDecisionsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfPathfindingsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfBedSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfTreeSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDropPointSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfSleepersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfHarvestersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfIdleDisplay;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _numberOfUnitsDisplay.gameObject.SetActive(_showNumberOfUnits);
            _numberOfDecisionsDisplay.gameObject.SetActive(_showNumberOfDecisions);
            _numberOfPathfindingsDisplay.gameObject.SetActive(_showNumberOfPathfindings);
            _numberOfBedSeekersDisplay.gameObject.SetActive(_showNumberOfBedSeekers);
            _numberOfTreeSeekersDisplay.gameObject.SetActive(_showNumberOfTreeSeekers);
            _numberOfDropPointSeekersDisplay.gameObject.SetActive(_showNumberOfDropPointsSeekers);
            _numberOfSleepersDisplay.gameObject.SetActive(_showNumberOfSleepers);
            _numberOfHarvestersDisplay.gameObject.SetActive(_showNumberOfHarvesters);
            _numberOfIdleDisplay.gameObject.SetActive(_showNumberOfIdle);
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
    }
}