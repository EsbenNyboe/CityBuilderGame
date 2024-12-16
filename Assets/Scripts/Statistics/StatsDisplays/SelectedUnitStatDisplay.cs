using TMPro;
using UnityEngine;

namespace Statistics.StatsDisplays
{
    public class SelectedUnitStatDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _statsValueText;
        private float _currentValueSum;
        private int _currentValueCount;

        private void Update()
        {
            _statsValueText.text = (_currentValueSum / _currentValueCount).ToString("n2");
            _currentValueSum = 0;
            _currentValueCount = 0;
        }

        public void AddValue(float newValue)
        {
            _currentValueSum += newValue;
            _currentValueCount++;
        }
    }
}