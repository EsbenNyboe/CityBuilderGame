using System.Text;
using TMPro;
using UnityEngine;

public class GlobalStatusDisplay : MonoBehaviour
{
    private static GlobalStatusDisplay _instance;
    [SerializeField] private TextMeshProUGUI _numberOfUnitsTextMeshProUGUI;
    [SerializeField] private string _numberOfUnitsPrependText;
    [SerializeField] private bool _showNumberOfUnits;

    private void Awake()
    {
        _instance = this;
        _numberOfUnitsTextMeshProUGUI.enabled = _showNumberOfUnits;
    }

    public static void SetNumberOfUnits(int numberOfUnits)
    {
        // TODO: Test if "new" is necessary
        _instance._numberOfUnitsTextMeshProUGUI.text =
            new StringBuilder().Append(_instance._numberOfUnitsPrependText).Append(numberOfUnits.ToString()).ToString();
    }
}