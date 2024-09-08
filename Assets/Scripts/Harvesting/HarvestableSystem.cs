using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public partial class HarvestableSystem : SystemBase
{
    private const float DegradationPerSec = 20;
    private Dictionary<Material, BatchMaterialID> m_MaterialMapping;

    protected override void OnStartRunning()
    {
        var hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        m_MaterialMapping = new Dictionary<Material, BatchMaterialID>();

        var degradationStagesMaterials = DegradationVisualsManager.Instance.GetMaterials();

        foreach (var material in degradationStagesMaterials)
        {
            RegisterMaterial(hybridRenderer, material);
        }
    }

    protected override void OnUpdate()
    {
        var degradeEverything = Input.GetKey(KeyCode.A);
        var degradationThisFrame = DegradationPerSec * SystemAPI.Time.DeltaTime;

        foreach (var (unitDegradation, localTransform, postTransformMatrix, materialMeshInfo, entity) in SystemAPI
                     .Query<RefRW<UnitDegradation>, RefRW<LocalTransform>, RefRW<PostTransformMatrix>,
                         RefRW<MaterialMeshInfo>>()
                     .WithEntityAccess())
        {
            unitDegradation.ValueRW.IsDegrading = degradeEverything;
            if (!unitDegradation.ValueRO.IsDegrading)
            {
                continue;
            }

            unitDegradation.ValueRW.Health -= degradationThisFrame;

            var normalizedHealth = unitDegradation.ValueRO.Health / unitDegradation.ValueRO.MaxHealth;

            if (normalizedHealth > 0)
            {
                var currentPosition = localTransform.ValueRO.Position;
                PathfindingGridSetup.Instance.pathfindingGrid.GetXY(currentPosition, out var x, out var y);
                var cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
                var cellPosition = PathfindingGridSetup.Instance.pathfindingGrid.GetWorldPosition(x, y);
                localTransform.ValueRW.Position = new float3
                {
                    x = currentPosition.x,
                    y = cellPosition.y + cellSize * 0.5f * (1 - normalizedHealth),
                    z = currentPosition.z,
                };

                postTransformMatrix.ValueRW.Value = float4x4.Scale(1, normalizedHealth, 1);

                if (normalizedHealth > 0.8f)
                {
                    // keep green color
                    continue;
                }

                if (normalizedHealth > 0.35f)
                {
                    // apply yellow color
                    materialMeshInfo.ValueRW.MaterialID =
                        m_MaterialMapping[DegradationVisualsManager.Instance.GetMaterials()[1]];
                    continue;
                }

                // apply red color
                materialMeshInfo.ValueRW.MaterialID =
                    m_MaterialMapping[DegradationVisualsManager.Instance.GetMaterials()[2]];
            }
            else
            {
                Debug.Log("Is dead");
            }
        }
    }


    private void RegisterMaterial(EntitiesGraphicsSystem hybridRendererSystem, Material material)
    {
        // Only register each mesh once, so we can also unregister each mesh just once
        if (!m_MaterialMapping.ContainsKey(material))
            m_MaterialMapping[material] = hybridRendererSystem.RegisterMaterial(material);
    }

    private void UnregisterMaterials()
    {
        // Can't call this from OnDestroy(), so we can't do this on teardown
        var hybridRenderer = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        if (hybridRenderer == null)
            return;

        foreach (var kv in m_MaterialMapping)
            hybridRenderer.UnregisterMaterial(kv.Value);
    }
}