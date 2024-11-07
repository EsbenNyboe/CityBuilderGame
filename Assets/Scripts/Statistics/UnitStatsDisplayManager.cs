using UnityEngine;

namespace Statistics
{
    public class UnitStatsDisplayManager : MonoBehaviour
    {
        public static UnitStatsDisplayManager Instance;
        [SerializeField] private bool _showNumberOfUnits;
        [SerializeField] private bool _showNumberOfDecisions;
        [SerializeField] private bool _showNumberOfBedSeekers;
        [SerializeField] private bool _showNumberOfTreeSeekers;

        [SerializeField] private UnitStatsDisplay _numberOfUnitsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfDecisionsDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfBedSeekersDisplay;
        [SerializeField] private UnitStatsDisplay _numberOfTreeSeekersDisplay;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _numberOfUnitsDisplay.gameObject.SetActive(_showNumberOfUnits);
            _numberOfDecisionsDisplay.gameObject.SetActive(_showNumberOfDecisions);
            _numberOfBedSeekersDisplay.gameObject.SetActive(_showNumberOfBedSeekers);
            _numberOfTreeSeekersDisplay.gameObject.SetActive(_showNumberOfTreeSeekers);
        }

        public void SetNumberOfUnits(int numberOfUnits)
        {
            _numberOfUnitsDisplay.SetStatsValue(numberOfUnits);
        }

        public void SetNumberOfDecidingUnits(int isDecidingCount)
        {
            _numberOfDecisionsDisplay.SetStatsValue(isDecidingCount);
        }

        public void SetNumberOfBedSeekingUnits(int isSeekingBedCount)
        {
            _numberOfBedSeekersDisplay.SetStatsValue(isSeekingBedCount);
        }

        public void SetNumberOfTreeSeekingUnits(int isSeekingTreeCount)
        {
            _numberOfTreeSeekersDisplay.SetStatsValue(isSeekingTreeCount);
        }
    }
}