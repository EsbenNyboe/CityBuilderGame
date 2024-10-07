using UnityEngine;

public class Globals : MonoBehaviour
{
    private static Globals _instance;

    [SerializeField] private float _gameSpeed = 1f;

    [SerializeField] private int _maxPathfindingPerFrame = 500;

    [SerializeField] private int _brushSize = 1;


    private void Awake()
    {
        _instance = this;
    }

    public static float GameSpeed()
    {
        return _instance._gameSpeed;
    }

    public static int MaxPathfindingPerFrame()
    {
        return _instance._maxPathfindingPerFrame;
    }

    public static int BrushSize()
    {
        return _instance._brushSize;
    }
}