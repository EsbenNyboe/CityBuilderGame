using GridEntityNS;
using Rendering;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Targeting.Core;
using Unity.Entities;
using UnityEngine;

namespace Fire
{
    public class BonfireAuthoring : MonoBehaviour
    {
        [SerializeField] private float _burnDuration;
        [SerializeField] private int _materialsRequired;
        [SerializeField] private WorldSpriteSheetEntryType _entryType;

        public class BonfireAuthoringBaker : Baker<BonfireAuthoring>
        {
            public override void Bake(BonfireAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Bonfire
                {
                    BurnTimeLeft = authoring._burnDuration,
                    IsBurning = false
                });
                AddComponent<GridEntity>(entity);
                AddComponent(entity, new Constructable
                {
                    MaterialsRequired = authoring._materialsRequired,
                    Materials = 0
                });
                AddComponent(entity, new Renderable
                {
                    EntryType = authoring._entryType
                });
                AddComponent<AutoConstruction>(entity);
                AddComponent<QuadrantEntity>(entity);
            }
        }
    }

    public struct Bonfire : IComponentData
    {
        public float BurnTimeLeft;
        public bool IsBurning;
    }
}