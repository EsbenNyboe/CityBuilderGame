using UnityEngine;

public class UnitStatsDisplayPerSecond : UnitStatsDisplay
{
    private float _valuePerSecond;

    private void Update()
    {
        _valuePerSecond *= 1 - Time.deltaTime;
    }

    protected override int GetTextValue(int rawValue)
    {
        _valuePerSecond += rawValue;
        var currentStatsValuePerSecond = Mathf.FloorToInt(_valuePerSecond);
        return currentStatsValuePerSecond;
    }
}