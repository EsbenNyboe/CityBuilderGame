using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class RenderTestAuthoring : MonoBehaviour
{
    [SerializeField] private Material _material;

    private class RenderTestBaker : Baker<RenderTestAuthoring>
    {
        public override void Bake(RenderTestAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var renderTest = new RenderTest
            {
                Material = authoring._material
            };
            AddComponentObject(entity, renderTest);
        }
    }
}

public class RenderTest : IComponentData
{
    public Material Material;
}