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
            GetSelectionUnitsBounds(out var xMin, out var xMax, out var yMin, out var yMax);
            GetCameraBounds(out var yTop, out var yBottom, out var xLeft, out var xRight);
            var maxDiffX = math.max(0, math.max(xMax - xRight, -(xMin - xLeft)));
            var maxDiffY = math.max(0, math.max(yMax - yTop, -(yMin - yBottom)));

            CameraController.Instance.FollowZoomAmount = math.max(maxDiffX, maxDiffY) / 1000f;

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelectedUnits();
            }
        }

        private void GetSelectionUnitsBounds(out float xMin, out float xMax, out float yMin, out float yMax)
        {
            xMin = 0;
            xMax = 0;
            yMin = 0;
            yMax = 0;
            foreach (var localTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<UnitSelection>())
            {
                var positionX = localTransform.ValueRO.Position.x;
                xMin = positionX < xMin ? positionX : xMin;
                xMax = positionX > xMax ? positionX : xMax;

                var positionY = localTransform.ValueRO.Position.y;
                yMin = positionY < yMin ? positionY : yMin;
                yMax = positionY > yMax ? positionY : yMax;
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

        private void GetCameraBounds(out float yTop, out float yBottom, out float xLeft, out float xRight)
        {
            var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
            var cameraPosition = cameraInformation.CameraPosition;
            var screenRatio = cameraInformation.ScreenRatio;
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