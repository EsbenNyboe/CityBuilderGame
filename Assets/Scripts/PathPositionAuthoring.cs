using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PathPositionAuthoring : MonoBehaviour
{
    [SerializeField] private int2 _position;

    public class Baker : Baker<PathPositionAuthoring>
    {
        public override void Bake(PathPositionAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddBuffer<PathPosition>(entity);
        }
    }
}