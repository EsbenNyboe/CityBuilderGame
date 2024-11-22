using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpriteSheetRendererManager : MonoBehaviour
{
    public static SpriteSheetRendererManager Instance;

    public Mesh UnitMesh;
    public Material UnitMaterial;

    [SerializeField] private AnimationConfig _animationConfig;

    private SystemHandle _unitAnimationManagerSystem;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _unitAnimationManagerSystem =
            World.DefaultGameObjectInjectionWorld.GetExistingSystem(typeof(UnitAnimationManagerSystem));
    }

    private void Update()
    {
        var unitAnimationManager =
            World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<UnitAnimationManager>(
                _unitAnimationManagerSystem);

        var uvScaleX = 1f / unitAnimationManager.SpriteColumns;
        var uvScaleY = 1f / unitAnimationManager.SpriteRows;

        var uv = new Vector4(uvScaleX, uvScaleY, 0, 0);
        var uvOffsetX = uvScaleX * 0;
        var uvOffsetY = uvScaleY * _animationConfig.SpriteRow;
        uv.z = uvOffsetX;
        uv.w = uvOffsetY;

        var position = UtilsClass.GetMouseWorldPosition();
        var rotation = quaternion.identity;

        var matrix4X4 = Matrix4x4.TRS(position, rotation, Vector3.one);

        DrawMesh(UnitMesh, UnitMaterial, uv, matrix4X4);
    }

    private static void DrawMesh(Mesh mesh, Material material, Vector4 uv,
        Matrix4x4 matrix4X4)
    {
        var uvInstancedArray = new [] { uv };
        var matrixInstancedArray = new [] { matrix4X4 };
        var mainTexUV = Shader.PropertyToID("_MainTex_UV");
        var materialPropertyBlock = new MaterialPropertyBlock();

        materialPropertyBlock.SetVectorArray(mainTexUV, uvInstancedArray);
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixInstancedArray, 1, materialPropertyBlock);
    }
}