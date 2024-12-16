using Inventory;
using Rendering;
using Rendering.SpriteTransformNS;
using UnitAgency.Data;
using UnitBehaviours.ActionGateNS;
using UnitBehaviours.Pathing;
using UnitBehaviours.Tags;
using UnitBehaviours.Targeting;
using UnitControl;
using UnitSpawn;
using UnitState.AliveState;
using UnitState.Mood;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours
{
    public class VillagerAuthoring : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private InventoryItem _startItem;
        [SerializeField] private float _sleepiness;
        [SerializeField] private float _restlessness = 1f;
        [SerializeField] private float _loneliness;
        [SerializeField] private float _initiative = 1f;

        public class VillagerAuthoringBaker : Baker<VillagerAuthoring>
        {
            public override void Bake(VillagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Villager>(entity);
                AddComponent<IsAlive>(entity);
                SetComponentEnabled<IsAlive>(entity, true);
                AddComponent<SpawnedUnit>(entity);
                AddComponent<RandomContainer>(entity);
                SetComponentEnabled<RandomContainer>(entity, false);
                AddComponent<ActionGate>(entity);

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
                AddComponent<WorldSpriteSheetAnimation>(entity);
                AddComponent<WorldSpriteSheetState>(entity);
                AddComponent<UnitAnimationSelection>(entity);

                AddComponent<IsDeciding>(entity);
                AddComponent(entity, new Health
                {
                    CurrentHealth = authoring._maxHealth,
                    MaxHealth = authoring._maxHealth
                });
                AddComponent(entity, new InventoryState { CurrentItem = authoring._startItem });
                AddComponent(entity, new MoodSleepiness { Sleepiness = authoring._sleepiness });
                AddComponent(entity, new MoodRestlessness { Restlessness = authoring._restlessness });
                AddComponent(entity, new MoodLoneliness { Loneliness = authoring._loneliness });
                AddComponent(entity, new MoodInitiative { Initiative = authoring._initiative });
            }
        }
    }
}