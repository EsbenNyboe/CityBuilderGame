using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Rendering;
using Unity.VisualScripting;
using UnityEngine;

public class RenderTestManager : MonoBehaviour
{
    [SerializeField]
    private Mesh _mesh;
    [SerializeField]
    private Material _material;

    private EntityManager entityManager;

    public static RenderTestManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    // private void Start()
    // {
    //     var system = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerShootingSystem>();
    //
    //     system.OnShoot += PlayerShootingSystem_OnShoot;
    // }
    //
    // private void PlayerShootingSystem_OnShoot(object sender, EventArgs e)
    // {
    //     var playerEntity = (Entity)sender;
    //     var localTransform = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<LocalTransform>(playerEntity);
    //     Instantiate(_shootPopupPrefab, localTransform.Position, quaternion.identity);
    // }

    public void ApplyRenderData(Entity spawnedEntity)
    {
        entityManager.AddComponent(spawnedEntity, typeof(RenderMeshUnmanaged));
        entityManager.SetComponentData(spawnedEntity, new RenderMeshUnmanaged
        {
            mesh = _mesh,
            materialForSubMesh = _material
        });
        //entityManager.SetSharedComponentManaged(spawnedEntity, new RenderMesh
        //{
        //    mesh = _mesh,
        //    Material = _material
        //});

        Debug.Log("HELOO?");
    }
}