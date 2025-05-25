using Grid;
using Rendering;
using SystemGroups;
using UnitBehaviours.Tags;
using UnitState.AliveState;
using Unity.Collections;
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
            var positions = new NativeArray<float3>(selectedCount, Allocator.Temp);
            var index = 0;
            foreach (var (_, localTransform) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>())
            {
                index++;
                var pos = localTransform.ValueRO.Position;
                positions[index] = pos;
                positionSum += pos;
                Graphics.DrawMesh(mesh, pos + positionOffset, Quaternion.identity, material, 0);
            }

            var averagePosition = positionSum / selectedCount;
            CameraController.Instance.FollowPosition = selectedCount <= 0 ? Vector3.zero : averagePosition;
//            var selectionUnitsBounds = 

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelectedUnits();
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
    }
}