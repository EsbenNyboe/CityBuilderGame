using Unity.Entities;
using UnityEngine;

public class PrefabEntityComponentAuthoring : MonoBehaviour
{
    public class Baker : Baker<PrefabEntityComponentAuthoring>
    {
        public override void Bake(PrefabEntityComponentAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new PrefabEntityComponent());
        }
    }
}

public struct PrefabEntityComponent : IComponentData
{
    public Entity prefabEntity;
}