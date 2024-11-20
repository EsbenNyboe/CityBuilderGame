using UnitAgency;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours
{
    public struct Villager : IComponentData
    {
    }

    public class VillagerAuthoring : MonoBehaviour
    {
        [SerializeField] private InventoryItem _startItem;
        [SerializeField] private float _sleepiness;
        [SerializeField] private float _restlessness;
        [SerializeField] private float _loneliness;
        [SerializeField] private float _initiative;

        public class VillagerAuthoringBaker : Baker<VillagerAuthoring>
        {
            public override void Bake(VillagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Villager>(entity);
                AddComponent<IsAlive>(entity);
                SetComponentEnabled<IsAlive>(entity, true);
                AddComponent<SpawnedUnit>(entity);

                AddComponent<Selectable>(entity);
                AddComponent<PathPosition>(entity);
                AddComponent(entity, new PathFollow
                {
                    PathIndex = -1,
                    MoveSpeedMultiplier = 1f
                });

                AddComponent<QuadrantEntity>(entity);
                AddComponent<TargetFollow>(entity);

                AddComponent(entity, new SpriteTransform { Position = new float3(), Rotation = quaternion.identity });
                AddComponent<SpriteSheetAnimation>(entity);
                AddComponent<UnitAnimationSelection>(entity);

                AddComponent<IsDeciding>(entity);
                AddComponent(entity, new Inventory { CurrentItem = authoring._startItem });
                AddComponent(entity, new MoodSleepiness { Sleepiness = authoring._sleepiness });
                AddComponent(entity, new MoodRestlessness { Restlessness = authoring._restlessness });
                AddComponent(entity, new MoodLoneliness { Loneliness = authoring._loneliness });
                AddComponent(entity, new MoodInitiative { Initiative = authoring._initiative });
            }
        }
    }
}