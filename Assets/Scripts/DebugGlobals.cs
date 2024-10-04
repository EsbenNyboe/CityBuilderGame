using UnityEngine;

public class DebugGlobals : MonoBehaviour
{
    public static DebugGlobals Instance;

    [SerializeField]
    private bool _showOccupationGrid = false;
    [SerializeField]

    private void Awake()
    {
        Instance = this;
    }

    public static bool ShowOccupationGrid()
    {
        return Instance._showOccupationGrid;
    }
}