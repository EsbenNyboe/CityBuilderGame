using Unity.Entities;
using UnityEngine;

public class SpriteSheetTestUnitAuthoring : MonoBehaviour
{
    public class Baker : Baker<SpriteSheetTestUnitAuthoring>
    {
        public override void Bake(SpriteSheetTestUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpriteSheetTestUnit());
        }
    }
}

public struct SpriteSheetTestUnit : IComponentData
{
}