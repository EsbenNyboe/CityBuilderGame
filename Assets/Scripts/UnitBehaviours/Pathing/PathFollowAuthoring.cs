using Unity.Entities;
using UnityEngine;

public class PathFollowAuthoring : MonoBehaviour
{
    [SerializeField] private int _pathIndex;

    public class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new PathFollow
            {
                PathIndex = authoring._pathIndex
            });
        }
    }
}

public struct PathFollow : IComponentData
{
    public int PathIndex;
}