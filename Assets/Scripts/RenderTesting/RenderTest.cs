using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class RenderTest : MonoBehaviour
{
    [SerializeField]
    private Mesh _mesh;
    [SerializeField]
    private Material _material;
     
    private EntityManager entityManager;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var entity = entityManager.CreateEntity(typeof(RenderMesh), typeof(LocalToWorld));
        entityManager.SetSharedComponentManaged(entity, new RenderMesh {mesh = _mesh, material = _material});
        // This doesn't work, because we can't convert a component to a shared component. Perhaps because RenderMesh is managed, and RenderMeshUnmanaged is not?
        //entityManager.SetSharedComponentManaged(entity, new RenderMeshUnmanaged {mesh = _mesh, materialForSubMesh = _material});
    }
}
