using Unity.Entities;
using UnityEngine;

public class PathFollowAuthoring : MonoBehaviour
{
    public class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new PathFollow());
        }
    }
}

public struct PathFollow : IComponentData
{
    public int pathIndex;
}