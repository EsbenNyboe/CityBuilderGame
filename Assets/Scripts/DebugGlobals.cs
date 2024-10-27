using UnityEngine;

public class DebugGlobals : MonoBehaviour
{
    public static DebugGlobals Instance;

    [SerializeField] private bool _showOccupiableGrid;
    [SerializeField] private bool _showWalkableGrid;

    private void Awake()
    {
        Instance = this;
    }

    public static bool ShowOccupationGrid()
    {
        return Instance._showOccupiableGrid;
    }

    public static bool ShowWalkableGrid()
    {
        return Instance._showWalkableGrid;
    }
}