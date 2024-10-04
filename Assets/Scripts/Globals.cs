using UnityEngine;

class Globals : MonoBehaviour
{
    public static Globals Instance;

    [SerializeField]
    private float _gameSpeed = 1f;
    [SerializeField]
    private int _maxPathfindingPerFrame = 500;

    private void Awake()
    {
        Instance = this;
    }

    public static float GameSpeed()
    {
        return Instance._gameSpeed;
    }

    public static int MaxPathfindingPerFrame()
    {
        return Instance._maxPathfindingPerFrame;
    }
}