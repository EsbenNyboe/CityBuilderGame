using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public partial class RenderTestSystem : SystemBase
{
    private Dictionary<Material, BatchMaterialID> m_MaterialMapping;

    protected override void OnCreate()
    {
        RequireForUpdate<RenderTest>();
    }

    protected override void OnStartRunning()
    {
        var hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        m_MaterialMapping = new Dictionary<Material, BatchMaterialID>();

        Entities
            .WithoutBurst()
            .ForEach((in RenderTest changer) =>
            {
                RegisterMaterial(hybridRenderer, changer.Material);
            }).Run();
    }

    protected override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }


        EntityManager entityManager = EntityManager;

        Entities
            .WithoutBurst()
            .ForEach((RenderTest changer, ref MaterialMeshInfo mmi) =>
            {
                var material = changer.Material;
                mmi.MaterialID = m_MaterialMapping[material];

                //for (var i = 0; i < renderTest.AmountToSpawn; i++)
                //{
                //    var spawnedEntity = EntityManager.Instantiate(renderTest.ObjectToSpawn);
                //    EntityManager.SetComponentData(spawnedEntity, new LocalTransform
                //    {
                //        Position = default,
                //        Scale = 1f,
                //        Rotation = quaternion.identity
                //    });

            }).Run();
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