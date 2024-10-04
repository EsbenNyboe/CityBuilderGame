using UnityEngine;

class Globals : MonoBehaviour
{
    public static Globals Instance;

    [SerializeField]
    private float _gameSpeed = 1f;
    [SerializeField]
    private float _harvestingSpeed = 1f;
    [SerializeField]
    private int _maxPathfindingPerFrame = 500;
    [SerializeField]
    private int _brushSize = 1;

    private void Awake()
    {
        Instance = this;
    }

    public static float GameSpeed()
    {
        return Instance._gameSpeed;
    }

    public static float HarvestingSpeed()
    {
        return Instance._harvestingSpeed;
    }

    public static int MaxPathfindingPerFrame()
    {
        return Instance._maxPathfindingPerFrame;
    }

    public static int BrushSize()
    {
        return Instance._brushSize;
    }
}