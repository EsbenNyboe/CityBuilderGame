using Statistics.StatsDisplays;
using UnityEngine;

namespace Statistics
{
    public class SelectedUnitStats : MonoBehaviour
    {
        public static SelectedUnitStats Instance;

        public SelectedUnitStatDisplay Sleepiness;
        public SelectedUnitStatDisplay Loneliness;

        private void Awake()
        {
            Instance = this;
        }

        public void SetActive(bool hasSelection)
        {
            gameObject.SetActive(hasSelection);
        }
    }
}