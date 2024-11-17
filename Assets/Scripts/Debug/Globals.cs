using UnityEngine;

public class Globals : MonoBehaviour
{
    private static Globals _instance;

    [SerializeField] private float _gameSpeed = 1f;

    private void Awake()
    {
        _instance = this;
    }

    public static float GameSpeed()
    {
        return _instance._gameSpeed;
    }
}