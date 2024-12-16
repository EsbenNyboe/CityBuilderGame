using UnityEngine;

namespace Statistics.StatsDisplays
{
    public class UnitStatsDisplayPerSecond : UnitStatsDisplay
    {
        private float _valuePerSecond;

        protected override int GetTextValue(int rawValue)
        {
            _valuePerSecond += rawValue;
            var currentStatsValuePerSecond = Mathf.FloorToInt(_valuePerSecond);
            return currentStatsValuePerSecond;
        }

        protected override void OnUpdate()
        {
            _valuePerSecond *= 1 - Time.deltaTime;
        }
    }
}