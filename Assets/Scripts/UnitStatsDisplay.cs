using System.Text;
using TMPro;
using UnityEngine;

public class UnitStatsDisplay : MonoBehaviour
{
    public static UnitStatsDisplay Instance;
    [SerializeField] private bool _showNumberOfUnits;

    [SerializeField] private TextMeshProUGUI _numberOfUnitsTextMeshProUGUI;
    [SerializeField] private string _numberOfUnitsPrependText;
    private readonly StringBuilder _sb = new ();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        _numberOfUnitsTextMeshProUGUI.gameObject.SetActive(_showNumberOfUnits);
    }

    public void SetNumberOfUnits(int numberOfUnits)
    {
        SetStringValue(Instance._numberOfUnitsTextMeshProUGUI, Instance._numberOfUnitsPrependText, numberOfUnits);
    }

    private void SetStringValue(TextMeshProUGUI textMeshProUGUI, string prependText, int numberOfUnits)
    {
        _sb.Clear();
        var text = _sb.Append(prependText).Append(numberOfUnits.ToString()).ToString();
        textMeshProUGUI.text = text;
    }
}