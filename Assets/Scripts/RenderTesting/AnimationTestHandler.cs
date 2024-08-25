using UnityEngine;

public class AnimationTestHandler : MonoBehaviour
{
    public Mesh Mesh;
    public Material Material;

    private static AnimationTestHandler _instance;
    void Awake()
    {
        _instance = this;
    }

    public static AnimationTestHandler GetInstance()
    {
        return _instance;
    }
}