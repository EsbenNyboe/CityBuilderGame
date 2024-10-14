using Unity.Entities;
using UnityEngine;

public class SelectableAuthoring : MonoBehaviour
{
    public class SelectableBaker : Baker<SelectableAuthoring>
    {
        public override void Bake(SelectableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Selectable>(entity);
        }
    }
}

public struct Selectable : IComponentData
{
}