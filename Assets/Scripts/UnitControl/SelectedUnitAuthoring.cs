using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class SelectedUnitAuthoring : MonoBehaviour
{
    private class Baker : Baker<SelectedUnitAuthoring>
    {
        public override void Bake(SelectedUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var renderTest = new SelectedUnit
            {
                Material = SelectionAreaManager.Instance.UnitSelectedMaterial,
                Mesh = SelectionAreaManager.Instance.UnitSelectedMesh
            };
            AddComponentObject(entity, renderTest);
        }
    }
}

public class SelectedUnit : IComponentData
{
    public Material Material;
    public Mesh Mesh;
    public bool Initialized;
}