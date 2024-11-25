using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    public class WorldSpriteSheetConfig : MonoBehaviour
    {
        public static WorldSpriteSheetConfig Instance;
        public Mesh UnitMesh;
        public Material UnitMaterial;

        public SpriteSheetEntry[] SpriteSheetEntries;

        public int ColumnCount;
        public int RowCount;

        [SerializeField] private AnimationId _previewAnimation;
        [SerializeField] private AnimationId[] _previewInventoryItems;
        [SerializeField] private float _stackOffsetFactor;

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
            if (_previewAnimation != AnimationId.None)
            {
                PreviewLogic();
            }
        }

        private void OnValidate()
        {
            IsDirty = true;
        }

        private void PreviewLogic()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var singletonQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldSpriteSheetManager>());
            if (!singletonQuery.TryGetSingleton<WorldSpriteSheetManager>(out var singleton) ||
                !singleton.Entries.IsCreated)
            {
                return;
            }

            if (_cameraController == default)
            {
                _cameraController = FindObjectOfType<CameraController>();
                _cameraController.SetMaxSize(1.8f);
            }

            var uvList = new List<Vector4>();
            var matrix4X4List = new List<Matrix4x4>();

            AddAnimationInfo(singleton, singleton.Entries[(int)_previewAnimation], ref uvList, ref matrix4X4List);

            if (_previewAnimation == AnimationId.IdleHolding || _previewAnimation == AnimationId.WalkHolding)
            {
                var stackAmount = 0;
                foreach (var previewInventoryItem in _previewInventoryItems)
                {
                    if (previewInventoryItem != AnimationId.None)
                    {
                        AddInventoryInfo(singleton, singleton.Entries[(int)previewInventoryItem], stackAmount, ref uvList, ref matrix4X4List);
                        stackAmount++;
                    }
                }
            }

            DrawMesh(UnitMesh, UnitMaterial, uvList.ToArray(), matrix4X4List.ToArray());
        }

        private void AddAnimationInfo(WorldSpriteSheetManager singleton, WorldSpriteSheetEntry singletonEntry,
            ref List<Vector4> uvList,
            ref List<Matrix4x4> matrix4X4List)
        {
            var currentFrame = CalculateCurrentFrame(singletonEntry.EntryColumns.Length, singletonEntry.FrameInterval);
            GetMeshConfiguration(singleton, singletonEntry, currentFrame, 0, out var uv, out var matrix4X4);
            uvList.Add(uv);
            matrix4X4List.Add(matrix4X4);
        }

        private void AddInventoryInfo(WorldSpriteSheetManager singleton, WorldSpriteSheetEntry singletonEntry, int stackAmount,
            ref List<Vector4> uvList,
            ref List<Matrix4x4> matrix4X4List)
        {
            var currentFrame = 0;
            GetMeshConfiguration(singleton, singletonEntry, currentFrame, stackAmount, out var uv, out var matrix4X4);
            uvList.Add(uv);
            matrix4X4List.Add(matrix4X4);
        }

        private void GetMeshConfiguration(WorldSpriteSheetManager singleton, WorldSpriteSheetEntry singletonEntry, int currentFrame, int stackAmount,
            out Vector4 uv, out Matrix4x4 matrix4X4)
        {
            var uvScaleX = singleton.ColumnScale;
            var uvScaleY = singleton.RowScale;

            uv = new Vector4(uvScaleX, uvScaleY, 0, 0);
            var uvOffsetX = uvScaleX * singletonEntry.EntryColumns[currentFrame];
            var uvOffsetY = uvScaleY * singletonEntry.EntryRows[currentFrame];
            uv.z = uvOffsetX;
            uv.w = uvOffsetY;

            var position = Camera.main.transform.position;
            position.y += stackAmount * _stackOffsetFactor;
            position.z = 0;
            var rotation = quaternion.identity;

            matrix4X4 = Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        private int CalculateCurrentFrame(int frameCount, float frameInterval)
        {
            if (_currentFrame >= frameCount)
            {
                _currentFrame = 0;
            }

            _frameTimer += Time.deltaTime;
            while (_frameTimer > frameInterval)
            {
                _frameTimer -= frameInterval;
                _currentFrame = (_currentFrame + 1) % frameCount;
            }

            return _currentFrame;
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
        [Min(1)] public int FrameCount;
        [Min(0.001f)] public float FrameInterval;
    }
}