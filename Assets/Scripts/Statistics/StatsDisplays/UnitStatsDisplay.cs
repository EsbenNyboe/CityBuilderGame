using TMPro;
using UnityEngine;

public abstract class UnitStatsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _statsValueText;
    private int _currentValue;
    private int _previousValue;

    private void Update()
    {
        OnUpdate();

        if (_currentValue != _previousValue)
        {
            _previousValue = _currentValue;
            _statsValueText.text = _currentValue.ToString();
        }
    }

    protected abstract int GetTextValue(int rawValue);

    public void SetStatsValue(int rawValue)
    {
        _currentValue = GetTextValue(rawValue);
    }

    protected abstract void OnUpdate() ;
}