using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;


[DisallowMultipleComponent]
public class RenderTestAuthoring : MonoBehaviour
{
    [SerializeField]
    private Material _material;

    class RenderTestBaker : Baker<RenderTestAuthoring>
    {
        public override void Bake(RenderTestAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var renderTest = new RenderTest
            {
                Material = authoring._material
            };
            AddComponentObject(entity, renderTest);
            //AddComponent(entity, new RenderTest
            //{
            //    ObjectToSpawn = GetEntity(authoring._objectToSpawn, TransformUsageFlags.Dynamic),
            //    AmountToSpawn = authoring._amountToSpawn,
            //});
        }
    }

    //public class Baker : Baker<RenderTestAuthoring>
    //{
    //    public override void Bake(RenderTestAuthoring authoring)
    //    {
    //        var entity = GetEntity(TransformUsageFlags.None);
    //        //SetComponent(entity, new RenderMeshUnmanaged
    //        //{
    //        //    mesh = authoring._mesh,
    //        //    materialForSubMesh = authoring._material,
    //        //});
    //    }
    //}

    //private void Start()
    //{
    //    entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    //    var entity = entityManager.CreateEntity(typeof(RenderMesh), typeof(LocalTransform), typeof(LocalToWorld));
    //    entityManager.SetSharedComponentManaged(entity, new RenderMesh { mesh = _mesh, Material = _material });
    //    entityManager.SetComponentData(entity, new LocalTransform
    //    {
    //        Position = default,
    //        Scale = 1,
    //        Rotation = Quaternion.identity
    //    });
    //    //entityManager.SetComponentData(entity, new RenderMeshUnmanaged
    //    //{
    //    //    mesh = default,
    //    //    materialForSubMesh = default
    //    //});
    //    // This doesn't work, because we can't convert a component to a shared component. Perhaps because RenderMesh is managed, and RenderMeshUnmanaged is not?
    //    //entityManager.SetSharedComponentManaged(entity, new RenderMeshUnmanaged {mesh = _mesh, materialForSubMesh = _material});
    //}
}

public class RenderTest : IComponentData
{
    public Material Material;
}
