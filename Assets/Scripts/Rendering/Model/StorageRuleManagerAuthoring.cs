using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    public struct StorageRuleManager : IComponentData
    {
        public WorldSpriteSheetEntryType EntryType;
        public float2 Padding;
        public float2 Spacing;
        public int MaxPerStack;
        public int MaxPerStructure;
    }

    public class StorageRuleManagerAuthoring : MonoBehaviour
    {
        public WorldSpriteSheetEntryType EntryType;
        public float2 Padding;
        public float2 Spacing;
        public int MaxPerStack;
        public int MaxPerStructure;

        public class StorageRuleManagerBaker : Baker<StorageRuleManagerAuthoring>
        {
            public override void Bake(StorageRuleManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var storageRuleManager = new StorageRuleManager
                {
                    EntryType = authoring.EntryType,
                    Padding = authoring.Padding,
                    Spacing = authoring.Spacing,
                    MaxPerStack = authoring.MaxPerStack,
                    MaxPerStructure = authoring.MaxPerStructure
                };
                AddComponent(entity, storageRuleManager);
            }
        }
    }
}