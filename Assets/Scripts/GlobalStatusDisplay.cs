using System.Text;
using TMPro;
using UnityEngine;

public class GlobalStatusDisplay : MonoBehaviour
{
    private static GlobalStatusDisplay _instance;
    [SerializeField] private TextMeshProUGUI _numberOfUnitsTextMeshProUGUI;
    [SerializeField] private TextMeshProUGUI _numberOfTreesTextMeshProUGUI;
    [SerializeField] private string _numberOfUnitsPrependText;
    [SerializeField] private string _numberOfTreesPrependText;
    [SerializeField] private bool _showNumberOfUnits;
    [SerializeField] private bool _showNumberOfTrees;

    private void Awake()
    {
        _instance = this;
        _numberOfUnitsTextMeshProUGUI.enabled = _showNumberOfUnits;
        _numberOfTreesTextMeshProUGUI.enabled = _showNumberOfTrees;
    }

    public static void SetNumberOfUnits(int numberOfUnits)
    {
        // TODO: Test if "new" is necessary
        _instance._numberOfUnitsTextMeshProUGUI.text =
            new StringBuilder().Append(_instance._numberOfUnitsPrependText).Append(numberOfUnits.ToString()).ToString();
    }

    public static void SetNumberOfTrees(int numberOfTrees)
    {
        _instance._numberOfTreesTextMeshProUGUI.text =
            new StringBuilder().Append(_instance._numberOfTreesPrependText).Append(numberOfTrees.ToString()).ToString();
    }
}