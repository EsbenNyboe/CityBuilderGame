using TMPro;
using UnityEngine;

public abstract class UnitStatsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _statsValueText;

    protected abstract int GetTextValue(int rawValue);

    public void SetStatsValue(int rawValue)
    {
        _statsValueText.text = GetTextValue(rawValue).ToString();
    }
}