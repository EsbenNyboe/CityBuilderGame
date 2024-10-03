using UnityEngine;

class Globals : MonoBehaviour
{
    public static Globals Instance;

    [SerializeField]
    private float _gameSpeed = 1f;

    private void Awake()
    {
        Instance = this;
    }

    public static float GameSpeed()
    {
        return Instance._gameSpeed;
    }
}