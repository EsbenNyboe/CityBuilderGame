using System.Collections.Generic;
using Rendering;
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

    [SerializeField] private WorldSpriteSheetEntryType _previewAnimation;
    [SerializeField] private WorldSpriteSheetEntryType[] _previewInventoryItems;

    [HideInInspector] public bool IsDirty = true;

    [SerializeField] private float _stackOffsetFactor;

    private CameraController _cameraController;
    private int _currentFrame;
    private float _frameTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (_previewAnimation == WorldSpriteSheetEntryType.None)
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
            if (_previewAnimation == AnimationConfigs[i].Identifier)
            {
                selectionIndex = i;
            }
        }

        if (selectionIndex < 0)
        {
            return;
        }

        var uvList = new List<Vector4>();
        var matrix4X4List = new List<Matrix4x4>();

        AddAnimationInfo(selectionIndex, ref uvList, ref matrix4X4List);

        if (AnimationConfigs[selectionIndex].Identifier == WorldSpriteSheetEntryType.IdleHolding ||
            AnimationConfigs[selectionIndex].Identifier == WorldSpriteSheetEntryType.WalkHolding)
        {
            var stackAmount = 0;
            foreach (var previewInventoryItem in _previewInventoryItems)
            {
                for (var i = 0; i < AnimationConfigs.Length; i++)
                {
                    if (previewInventoryItem == AnimationConfigs[i].Identifier)
                    {
                        selectionIndex = i;
                    }
                }

                AddInventoryInfo(selectionIndex, ref uvList, ref matrix4X4List, stackAmount);
                stackAmount++;
            }
        }

        DrawMesh(UnitMesh, UnitMaterial, uvList.ToArray(), matrix4X4List.ToArray());
    }

    private void OnValidate()
    {
        IsDirty = true;
    }

    private void AddAnimationInfo(int selectionIndex, ref List<Vector4> uvList, ref List<Matrix4x4> matrix4X4List)
    {
        var currentFrame = CalculateCurrentFrame(AnimationConfigs[selectionIndex]);
        GetMeshConfiguration(SpriteColumns, SpriteRows, currentFrame, AnimationConfigs[selectionIndex], 0,
            out var uv, out var matrix4X4);
        uvList.Add(uv);
        matrix4X4List.Add(matrix4X4);
    }

    private void AddInventoryInfo(int selectionIndex, ref List<Vector4> uvList, ref List<Matrix4x4> matrix4X4List,
        int stackAmount)
    {
        GetMeshConfiguration(SpriteColumns, SpriteRows, 0, AnimationConfigs[selectionIndex], stackAmount,
            out var uv, out var matrix4X4);
        uvList.Add(uv);
        matrix4X4List.Add(matrix4X4);
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

    private void GetMeshConfiguration(int spriteColumns, int spriteRows, int currentFrame,
        AnimationConfig animationConfig,
        int stackOffset,
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
        position.y += stackOffset * _stackOffsetFactor;
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