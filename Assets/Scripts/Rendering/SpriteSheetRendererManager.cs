using UnityEngine;

public class SpriteSheetRendererManager : MonoBehaviour
{
    public static SpriteSheetRendererManager Instance;
    public Mesh UnitMesh;
    public Material UnitWalking;

    private void Awake()
    {
        Instance = this;
    }
}