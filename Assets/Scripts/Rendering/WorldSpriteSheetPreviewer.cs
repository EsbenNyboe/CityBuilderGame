using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    public class WorldSpriteSheetPreviewer : MonoBehaviour
    {
        [SerializeField] private WorldSpriteSheetEntryType _previewAnimation;
        [SerializeField] private WorldSpriteSheetEntryType[] _previewInventoryItems;
        [SerializeField] private float _stackOffsetFactor;


        private CameraController _cameraController;
        private int _currentFrame;
        private float _frameTimer;


        private void Update()
        {
            if (_previewAnimation != WorldSpriteSheetEntryType.None)
            {
                PreviewLogic();
            }
        }

        private void PreviewLogic()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var singletonQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldSpriteSheetManager>());
            if (!singletonQuery.TryGetSingleton<WorldSpriteSheetManager>(out var singleton) ||
                !singleton.IsInitialized())
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

            if (_previewAnimation == WorldSpriteSheetEntryType.IdleHolding || _previewAnimation == WorldSpriteSheetEntryType.WalkHolding)
            {
                var stackAmount = 0;
                foreach (var previewInventoryItem in _previewInventoryItems)
                {
                    if (previewInventoryItem != WorldSpriteSheetEntryType.None)
                    {
                        AddInventoryInfo(singleton, singleton.Entries[(int)previewInventoryItem], stackAmount, ref uvList, ref matrix4X4List);
                        stackAmount++;
                    }
                }
            }

            WorldSpriteSheetRendererSystem.DrawMesh(new MaterialPropertyBlock(),
                WorldSpriteSheetConfig.Instance.UnitMesh,
                WorldSpriteSheetConfig.Instance.UnitMaterial,
                uvList.ToArray(),
                matrix4X4List.ToArray());
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
    }
}