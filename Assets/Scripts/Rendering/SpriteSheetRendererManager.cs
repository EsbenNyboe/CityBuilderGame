using Unity.Mathematics;
using UnityEngine;

public class SpriteSheetRendererManager : MonoBehaviour
{
    public static SpriteSheetRendererManager Instance;

    public Mesh UnitMesh;
    public Material UnitMaterial;
    public AnimationConfig[] AnimationConfigs;

    public int SpriteColumns = 3;
    public int SpriteRows = 4;

    [SerializeField] private AnimationId _previewSelection;

    [HideInInspector] public bool IsDirty = true;

    private CameraController _cameraController;
    private int _currentFrame;
    private float _frameTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (_previewSelection == AnimationId.None)
        {
            return;
        }

        if (_cameraController == default)
        {
            _cameraController = FindObjectOfType<CameraController>();
            _cameraController.SetMaxSize(1.8f);
        }

        var selectionIndex = -1;
        for (var i = 0; i < AnimationConfigs.Length; i++)
        {
            if (_previewSelection == AnimationConfigs[i].Identifier)
            {
                selectionIndex = i;
            }
        }

        if (selectionIndex < 0)
        {
            return;
        }

        var currentFrame = CalculateCurrentFrame(AnimationConfigs[selectionIndex]);
        GetMeshConfiguration(SpriteColumns, SpriteRows, currentFrame, AnimationConfigs[selectionIndex], out var uv,
            out var matrix4X4);
        DrawMesh(UnitMesh, UnitMaterial, new [] { uv }, new [] { matrix4X4 });
    }

    private void OnValidate()
    {
        IsDirty = true;
    }

    private int CalculateCurrentFrame(AnimationConfig animationConfig)
    {
        if (_currentFrame >= animationConfig.FrameCount)
        {
            _currentFrame = 0;
        }

        _frameTimer += Time.deltaTime;
        while (_frameTimer > animationConfig.FrameInterval)
        {
            _frameTimer -= animationConfig.FrameInterval;
            _currentFrame = (_currentFrame + 1) % animationConfig.FrameCount;
        }

        return _currentFrame;
    }

    private static void GetMeshConfiguration(int spriteColumns, int spriteRows, int currentFrame,
        AnimationConfig animationConfig,
        out Vector4 uv,
        out Matrix4x4 matrix4X4)
    {
        var uvScaleX = 1f / spriteColumns;
        var uvScaleY = 1f / spriteRows;

        uv = new Vector4(uvScaleX, uvScaleY, 0, 0);
        var uvOffsetX = uvScaleX * currentFrame;
        var uvOffsetY = uvScaleY * GetReversedSpriteRow(animationConfig, spriteRows);
        uv.z = uvOffsetX;
        uv.w = uvOffsetY;

        var position = Camera.main.transform.position;
        position.z = 0;
        var rotation = quaternion.identity;

        matrix4X4 = Matrix4x4.TRS(position, rotation, Vector3.one);
    }

    private static int GetReversedSpriteRow(AnimationConfig animationConfig, int rowCount)
    {
        animationConfig.SpriteRow = rowCount - animationConfig.SpriteRow - 1;
        return animationConfig.SpriteRow;
    }

    private static void DrawMesh(Mesh mesh, Material material, Vector4[] uvArray,
        Matrix4x4[] matrix4X4Array)
    {
        var mainTexUV = Shader.PropertyToID("_MainTex_UV");
        var materialPropertyBlock = new MaterialPropertyBlock();

        materialPropertyBlock.SetVectorArray(mainTexUV, uvArray);
        Graphics.DrawMeshInstanced(mesh, 0, material, matrix4X4Array, uvArray.Length, materialPropertyBlock);
    }
}