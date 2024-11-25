using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    public class SpriteSheetManagerConfigExperiment : MonoBehaviour
    {
        public static SpriteSheetManagerConfigExperiment Instance;
        public Mesh UnitMesh;
        public Material UnitMaterial;

        public SpriteSheetEntry[] SpriteSheetEntries;

        public int ColumnCount;
        public int RowCount;

        [SerializeField] private AnimationId _previewAnimation;
        [SerializeField] private AnimationId[] _previewInventoryItems;

        [SerializeField] private float _stackOffsetFactor;

        public bool IsDirty = true;

        private CameraController _cameraController;
        private int _currentFrame;
        private float _frameTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_previewAnimation == AnimationId.None)
            {
                return;
            }

            if (_cameraController == default)
            {
                _cameraController = FindObjectOfType<CameraController>();
                _cameraController.SetMaxSize(1.8f);
            }

            var currentColumn = 0;
            var currentRow = RowCount - 1;
            for (var i = 0; i < SpriteSheetEntries.Length; i++)
            {
                SpriteSheetEntries[i].StartColumn = currentColumn;
                SpriteSheetEntries[i].StartRow = currentRow;
                var frameCount = SpriteSheetEntries[i].FrameCount;
                currentColumn += frameCount;
                if (currentColumn >= ColumnCount)
                {
                    currentColumn = currentColumn % ColumnCount;
                    currentRow--;
                    if (currentRow < 0 && i == 0)
                    {
                        Debug.LogError("SpriteSheetEntry has invalid setup: Not enough rows!");
                    }
                }
            }

            var selectionIndex = -1;
            for (var i = 0; i < SpriteSheetEntries.Length; i++)
            {
                if (_previewAnimation == SpriteSheetEntries[i].Identifier)
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

            if (SpriteSheetEntries[selectionIndex].Identifier == AnimationId.IdleHolding ||
                SpriteSheetEntries[selectionIndex].Identifier == AnimationId.WalkHolding)
            {
                var stackAmount = 0;
                foreach (var previewInventoryItem in _previewInventoryItems)
                {
                    for (var i = 0; i < SpriteSheetEntries.Length; i++)
                    {
                        if (previewInventoryItem == SpriteSheetEntries[i].Identifier)
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
            var currentFrame = CalculateCurrentFrame(SpriteSheetEntries[selectionIndex]);
            GetMeshConfiguration(ColumnCount, RowCount, currentFrame, SpriteSheetEntries[selectionIndex], 0,
                out var uv, out var matrix4X4);
            uvList.Add(uv);
            matrix4X4List.Add(matrix4X4);
        }

        private void AddInventoryInfo(int selectionIndex, ref List<Vector4> uvList, ref List<Matrix4x4> matrix4X4List,
            int stackAmount)
        {
            GetMeshConfiguration(ColumnCount, RowCount, 0, SpriteSheetEntries[selectionIndex], stackAmount,
                out var uv, out var matrix4X4);
            uvList.Add(uv);
            matrix4X4List.Add(matrix4X4);
        }

        private int CalculateCurrentFrame(SpriteSheetEntry spriteSheetEntry)
        {
            if (_currentFrame >= spriteSheetEntry.FrameCount)
            {
                _currentFrame = 0;
            }

            _frameTimer += Time.deltaTime;
            while (_frameTimer > spriteSheetEntry.FrameInterval)
            {
                _frameTimer -= spriteSheetEntry.FrameInterval;
                _currentFrame = (_currentFrame + 1) % spriteSheetEntry.FrameCount;
            }

            return _currentFrame;
        }

        private void GetMeshConfiguration(int columnCount, int rowCount, int currentFrame,
            SpriteSheetEntry spriteSheetEntry,
            int stackOffset,
            out Vector4 uv,
            out Matrix4x4 matrix4X4)
        {
            var uvScaleX = 1f / columnCount;
            var uvScaleY = 1f / rowCount;

            GetCurrentColumnAndRow(columnCount, spriteSheetEntry, currentFrame, out var currentColumn, out var currentRow);
            uv = new Vector4(uvScaleX, uvScaleY, 0, 0);
            var uvOffsetX = uvScaleX * currentColumn;
            var uvOffsetY = uvScaleY * currentRow;
            uv.z = uvOffsetX;
            uv.w = uvOffsetY;

            var position = Camera.main.transform.position;
            position.y += stackOffset * _stackOffsetFactor;
            position.z = 0;
            var rotation = quaternion.identity;

            matrix4X4 = Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        private void GetCurrentColumnAndRow(int columnCount, SpriteSheetEntry spriteSheetEntry, int currentFrame, out int currentColumn,
            out int currentRow)
        {
            currentColumn = spriteSheetEntry.StartColumn;
            currentRow = spriteSheetEntry.StartRow;
            var frameIndex = 0;
            while (frameIndex < currentFrame)
            {
                frameIndex++;
                currentColumn++;
                if (currentColumn >= columnCount)
                {
                    currentColumn = 0;
                    currentRow--;
                    if (currentRow < 0 && frameIndex < currentFrame)
                    {
                        Debug.LogError("SpriteSheetEntry has invalid setup: Not enough rows!");
                    }
                }
            }
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

    [Serializable]
    public struct SpriteSheetEntry
    {
        public AnimationId Identifier;
        public int FrameCount;
        public float FrameInterval;
        [HideInInspector] public int StartColumn;
        [HideInInspector] public int StartRow;
    }
}