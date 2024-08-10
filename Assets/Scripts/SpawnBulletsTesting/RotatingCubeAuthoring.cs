using Unity.Entities;
using UnityEngine;

public class RotatingCubeAuthoring : MonoBehaviour
{
    public class Baker : Baker<RotatingCubeAuthoring>
    {
        public override void Bake(RotatingCubeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RotatingCube());
        }
    }
}

public struct RotatingCube : IComponentData
{
}