using GridEntityNS;
using Rendering;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Targeting.Core;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    public class DropPointAuthoring : MonoBehaviour
    {
        public int MaterialsRequired;
        public WorldSpriteSheetEntryType EntryType;

        public class Baker : Baker<DropPointAuthoring>
        {
            public override void Bake(DropPointAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<DropPoint>(entity);
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

    public struct DropPoint : IComponentData
    {
    }
}