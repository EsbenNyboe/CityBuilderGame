using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public partial class RenderTestSystem : SystemBase
{
    private Dictionary<Material, BatchMaterialID> _mMaterialMapping;

    protected override void OnCreate()
    {
        RequireForUpdate<RenderTest>();
    }

    protected override void OnStartRunning()
    {
        var hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        _mMaterialMapping = new Dictionary<Material, BatchMaterialID>();

        Entities
            .WithoutBurst()
            .ForEach((in RenderTest changer) => { RegisterMaterial(hybridRenderer, changer.Material); }).Run();
    }

    protected override void OnUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }


        var entityManager = EntityManager;

        Entities
            .WithoutBurst()
            .ForEach((RenderTest changer, ref MaterialMeshInfo mmi) =>
            {
                var material = changer.Material;
                mmi.MaterialID = _mMaterialMapping[material];
            }).Run();
    }

    private void RegisterMaterial(EntitiesGraphicsSystem hybridRendererSystem, Material material)
    {
        // Only register each mesh once, so we can also unregister each mesh just once
        if (!_mMaterialMapping.ContainsKey(material))
        {
            _mMaterialMapping[material] = hybridRendererSystem.RegisterMaterial(material);
        }
    }

    private void UnregisterMaterials()
    {
        // Can't call this from OnDestroy(), so we can't do this on teardown
        var hybridRenderer = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        if (hybridRenderer == null)
        {
            return;
        }

        foreach (var kv in _mMaterialMapping)
        {
            hybridRenderer.UnregisterMaterial(kv.Value);
        }
    }
}