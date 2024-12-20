using Inventory;
using Rendering;
using SpriteTransformNS;
using UnitAgency.Data;
using UnitBehaviours.ActionGateNS;
using UnitBehaviours.Pathing;
using UnitBehaviours.Tags;
using UnitBehaviours.Targeting;
using UnitBehaviours.Targeting.Core;
using UnitState.AliveState;
using UnitState.Mood;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours.UnitConfigurators
{
    public class BoarAuthoring : MonoBehaviour
    {
        [SerializeField] private float _maxHealth;
        [SerializeField] private float _restlessness;

        public class BoarBaker : Baker<BoarAuthoring>
        {
            public override void Bake(BoarAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Boar>(entity);
                // TODO: Cleanup this, so boar is not coupled with unnecessary IsAlive-logic
                AddComponent<IsAlive>(entity);
                SetComponentEnabled<IsAlive>(entity, true);
                AddComponent<RandomContainer>(entity);
                SetComponentEnabled<RandomContainer>(entity, false);
                AddComponent<ActionGate>(entity);

                AddComponent<InventoryState>(entity); // LOL!!!

                AddComponent<Selectable>(entity);
                AddComponent<PathPosition>(entity);
                AddComponent(entity, new PathFollow
                {
                    PathIndex = -1,
                    MoveSpeedMultiplier = 1f
                });

                AddComponent(entity, new SpriteTransform { Position = new float3(), Rotation = quaternion.identity });
                AddComponent<WorldSpriteSheetAnimation>(entity);
                AddComponent<WorldSpriteSheetState>(entity);
                AddComponent<UnitAnimationSelection>(entity);

                AddComponent<QuadrantEntity>(entity);
                AddComponent<TargetFollow>(entity);

                AddComponent(entity, new Health
                {
                    CurrentHealth = authoring._maxHealth,
                    MaxHealth = authoring._maxHealth
                });

                AddComponent<IsDeciding>(entity);

                AddComponent(entity, new MoodRestlessness
                {
                    Restlessness = authoring._restlessness
                });
                AddComponent(entity, new MoodInitiative());
            }
        }
    }
}