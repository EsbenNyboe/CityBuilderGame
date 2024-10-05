using UnityEngine;

public class SpriteSheetRendererManager : MonoBehaviour
{
    public static SpriteSheetRendererManager Instance;
    public Mesh UnitMesh;
    public Material UnitWalk;
    public Material UnitIdle;

    private void Awake()
    {
        Instance = this;
    }
}