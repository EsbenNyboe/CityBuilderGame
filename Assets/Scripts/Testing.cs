using Unity.Entities;
using UnityEngine;

public class Testing : MonoBehaviour
{
    private void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CreateEntity();
    }
}