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
            var positionSum = float3.zero;

            var positionOffset = new float3(0, -0.4f, 0);
            var selectedCount = _query.CalculateEntityCount();
            foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
            {
                var pos = localTransform.ValueRO.Position;
                positionSum += pos;
                Graphics.DrawMesh(mesh, pos + positionOffset, Quaternion.identity, material, 0);
            }

            var averagePosition = positionSum / selectedCount;
            CameraController.Instance.FollowPosition = selectedCount <= 0 ? Vector3.zero : averagePosition;
            GetCameraBounds(out var yTop, out var yBottom, out var xLeft, out var xRight, out var cameraPosition, out var screenRatio);
            GetSelectionUnitsBounds(cameraPosition, out var xMin, out var xMax, out var yMin, out var yMax);
            var maxDiffX = math.max(0, math.max(xMax - xRight, -(xMin - xLeft))) / screenRatio;
            var maxDiffY = math.max(0, math.max(yMax - yTop, -(yMin - yBottom)));

            CameraController.Instance.FollowZoomAmount = math.max(maxDiffX, maxDiffY) / 100f;

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelectedUnits();
            }
        }

        private void GetSelectionUnitsBounds(float3 cameraPosition, out float xMin, out float xMax, out float yMin, out float yMax)
        {
            xMin = cameraPosition.x;
            xMax = cameraPosition.x;
            yMin = cameraPosition.y;
            yMax = cameraPosition.y;
            foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
            {
                var positionX = localTransform.ValueRO.Position.x;
                xMin = positionX < xMin ? positionX - 2 : xMin;
                xMax = positionX > xMax ? positionX + 2 : xMax;

                var positionY = localTransform.ValueRO.Position.y;
                yMin = positionY < yMin ? positionY - 2 : yMin;
                yMax = positionY > yMax ? positionY + 2 : yMax;
            }
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