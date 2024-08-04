using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MovementAuthoring : MonoBehaviour
{
    public class Baker : Baker<MovementAuthoring>
    {
        public override void Bake(MovementAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Movement
            {
                MovementVector = new float3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f))
            });
        }
    }
}

public struct Movement : IComponentData
{
    public float3 MovementVector;
}