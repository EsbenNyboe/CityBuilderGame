using System.Text;
using TMPro;
using UnityEngine;

public class UnitStatsDisplay : MonoBehaviour
{
    public static UnitStatsDisplay Instance;
    [SerializeField] private bool _showNumberOfUnits;
    [SerializeField] private bool _showNumberOfDecisions;

    [SerializeField] private TextMeshProUGUI _numberOfUnitsTextMeshProUGUI;
    [SerializeField] private TextMeshProUGUI _numberOfDecisionsTextMeshProUGUI;
    [SerializeField] private string _numberOfUnitsPrependText;
    [SerializeField] private string _numberOfDecisionsPrependText;
    private readonly StringBuilder _sb = new ();

    private float _numberOfDecisionsDuringLastSecond;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        _numberOfUnitsTextMeshProUGUI.gameObject.SetActive(_showNumberOfUnits);

        _numberOfDecisionsDuringLastSecond *= 1 - Time.deltaTime;
    }

    public void SetNumberOfUnits(int numberOfUnits)
    {
        SetStringValue(_numberOfUnitsTextMeshProUGUI, _numberOfUnitsPrependText, numberOfUnits);
    }

    public void SetNumberOfDecidingUnits(int isDecidingCount)
    {
        _numberOfDecisionsDuringLastSecond += isDecidingCount;
        var numberOfDecisions = Mathf.FloorToInt(_numberOfDecisionsDuringLastSecond);
        SetStringValue(_numberOfDecisionsTextMeshProUGUI, _numberOfDecisionsPrependText, numberOfDecisions);
    }

    private void SetStringValue(TextMeshProUGUI textMeshProUGUI, string prependText, int numberOfUnits)
    {
        _sb.Clear();
        var text = _sb.Append(prependText).Append(numberOfUnits.ToString()).ToString();
        textMeshProUGUI.text = text;
    }
}