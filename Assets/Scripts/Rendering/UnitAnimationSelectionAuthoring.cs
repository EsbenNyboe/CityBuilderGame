using Unity.Entities;
using UnityEngine;

public class UnitAnimationSelectionAuthoring : MonoBehaviour
{
    public class Baker : Baker<UnitAnimationSelectionAuthoring>
    {
        public override void Bake(UnitAnimationSelectionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAnimationSelection());
        }
    }
}

public struct UnitAnimationSelection : IComponentData
{
    public int SelectedAnimation;
    public int CurrentAnimation;
}