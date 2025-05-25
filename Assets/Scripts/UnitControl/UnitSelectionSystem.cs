using Grid;
using Rendering;
using SystemGroups;
using UnitBehaviours.Tags;
using UnitState.AliveState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitControl
{
    [UpdateInGroup(typeof(UnitStateSystemGroup))]
    public partial class UnitSelectionSystem : SystemBase
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            _query = GetEntityQuery(typeof(UnitSelection));
        }

        protected override void OnUpdate()
        {
            var material = SelectionAreaManager.Instance.UnitSelectedMaterial;
            var mesh = SelectionAreaManager.Instance.UnitSelectedMesh;

            CameraController.Instance.FollowPosition = Vector3.zero;

            var positionOffset = new float3(0, -0.4f, 0);
            // var selectedCount = _query.CalculateEntityCount();
            foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
            {
                var pos = localTransform.ValueRO.Position;
                Graphics.DrawMesh(mesh, pos + positionOffset, Quaternion.identity, material, 0);
            }

            GetCameraBounds(out var screenTop, out var screenBottom, out var screenLeft, out var screenRight, out var cameraPosition,
                out var screenRatio);
            if (GetSelectionUnitsBounds(out var unitLeft, out var unitRight, out var unitBottom, out var unitTop))
            {
                var unitCenter = GetUnitSelectionCenter(unitLeft, unitRight, unitBottom, unitTop);

                var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
                var size = cameraInformation.OrthographicSize;
                var screenCenter = cameraInformation.CameraPosition;

                const float buffer = 2f;
                var boundsToSizeRatio = size / (screenTop - screenCenter.y);
                var unitSize = boundsToSizeRatio * (unitTop + buffer - unitCenter.y);

                CameraController.Instance.FollowPosition = unitCenter;
                CameraController.Instance.FollowZoomSize = unitSize;
                // CameraController.Instance.FollowZoomSize = screenExceedsUnitBounds ? -1 : 1;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelectedUnits();
            }
        }

        private Vector3 GetUnitSelectionCenter(float xMin, float xMax, float yMin, float yMax)
        {
            var xCenter = xMin + (xMax - xMin) * 0.5f;
            var yCenter = yMin + (yMax - yMin) * 0.5f;
            return new Vector3(xCenter, yCenter, 0);
        }

        private bool GetSelectionUnitsBounds(out float xMin, out float xMax, out float yMin, out float yMax)
        {
            var positionFound = false;
            xMin = xMax = yMin = yMax = 0;
            foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
            {
                var positionX = localTransform.ValueRO.Position.x;
                var positionY = localTransform.ValueRO.Position.y;

                if (!positionFound)
                {
                    positionFound = true;
                    xMin = positionX;
                    xMax = positionX;
                    yMin = positionY;
                    yMax = positionY;
                }

                xMin = positionX < xMin ? positionX : xMin;
                xMax = positionX > xMax ? positionX : xMax;
                yMin = positionY < yMin ? positionY : yMin;
                yMax = positionY > yMax ? positionY : yMax;
            }

            return positionFound;
        }

        private void DeleteSelectedUnits()
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<IsAlive>(entity, false);
            }

            SystemAPI.SetSingleton(gridManager);
        }

        private void GetCameraBounds(out float yTop, out float yBottom, out float xLeft, out float xRight, out float3 cameraPosition,
            out float screenRatio)
        {
            var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
            cameraPosition = cameraInformation.CameraPosition;
            screenRatio = cameraInformation.ScreenRatio;
            var orthographicSize = cameraInformation.OrthographicSize;

            var cullBuffer = 1f; // We add some buffer, so culling is not noticable
            var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
            var cameraSizeY = orthographicSize + cullBuffer;

            xLeft = cameraPosition.x - cameraSizeX;
            xRight = cameraPosition.x + cameraSizeX;
            yTop = cameraPosition.y + cameraSizeY;
            yBottom = cameraPosition.y - cameraSizeY;
        }
    }
}