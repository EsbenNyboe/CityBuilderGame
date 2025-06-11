using GridEntityNS;
using Inventory;
using Rendering;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Targeting.Core;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    public class StorageAuthoring : MonoBehaviour
    {
        public int MaterialsRequired;
        public WorldSpriteSheetEntryType EntryType;

        public class Baker : Baker<StorageAuthoring>
        {
            public override void Bake(StorageAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                var storage = AddBuffer<Storage>(entity);
                for (var i = 0; i < 12; i++)
                {
                    storage.Add(new Storage
                    {
                        Item = InventoryItem.None
                    });
                }

                AddComponent<GridEntity>(entity);
                AddComponent(entity, new Constructable
                {
                    MaterialsRequired = authoring.MaterialsRequired,
                    Materials = 0
                });
                AddComponent(entity, new Renderable
                {
                    EntryType = authoring.EntryType
                });
                AddComponent<AutoConstruction>(entity);
                AddComponent<QuadrantEntity>(entity);
            }
        }
    }

    [InternalBufferCapacity(12)]
    public struct Storage : IBufferElementData
    {
        public InventoryItem Item;
    }
}