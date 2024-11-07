using TMPro;
using UnityEngine;

public class UnitStatsDisplay : MonoBehaviour
{
    public static UnitStatsDisplay Instance;
    [SerializeField] private bool _showNumberOfUnits;
    [SerializeField] private bool _showNumberOfDecisions;

    [SerializeField] private TextMeshProUGUI _numberOfUnitsTextMeshProUGUI;
    [SerializeField] private TextMeshProUGUI _numberOfDecisionsTextMeshProUGUI;

    [SerializeField] private GameObject _numberOfUnitsDisplay;
    [SerializeField] private GameObject _numberOfDecisionsDisplay;

    private float _numberOfDecisionsDuringLastSecond;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        _numberOfUnitsDisplay.SetActive(_showNumberOfUnits);
        _numberOfDecisionsDisplay.SetActive(_showNumberOfDecisions);

        _numberOfDecisionsDuringLastSecond *= 1 - Time.deltaTime;
    }

    public void SetNumberOfUnits(int numberOfUnits)
    {
        SetStringValue(_numberOfUnitsTextMeshProUGUI, numberOfUnits);
    }

    public void SetNumberOfDecidingUnits(int isDecidingCount)
    {
        _numberOfDecisionsDuringLastSecond += isDecidingCount;
        var numberOfDecisions = Mathf.FloorToInt(_numberOfDecisionsDuringLastSecond);
        SetStringValue(_numberOfDecisionsTextMeshProUGUI, numberOfDecisions);
    }

    private void SetStringValue(TextMeshProUGUI textMeshProUGUI, int count)
    {
        textMeshProUGUI.text = count.ToString();
    }
}