using Audio;
using CustomTimeCore;
using Effects;
using Rendering;
using UnitBehaviours.UnitConfigurators;
using UnitState.AliveState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public partial struct SpearSystem : ISystem
    {
        private const float SpearSpeed = 15f;
        private const float SpearDamageRadius = 1.5f;
        private const float SpearDamage = 10f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }


        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (spear, entity) in SystemAPI.Query<RefRW<Spear>>().WithEntityAccess())
            {
                var position = spear.ValueRO.CurrentPosition;

                var distanceBeforeMoving = math.distance(position, spear.ValueRO.Target);
                spear.ValueRW.CurrentPosition = position += spear.ValueRO.Direction * SpearSpeed * SystemAPI.Time.DeltaTime * timeScale;
                var distanceAfterMoving = math.distance(position, spear.ValueRO.Target);
                if (distanceBeforeMoving <= distanceAfterMoving)
                {
                    ecb.DestroyEntity(entity);

                    var spearPosition2D = position;
                    var spearPosition = new float3(spearPosition2D.x, spearPosition2D.y, 0);

                    foreach (var (_, localTransform, health, boarEntity) in SystemAPI.Query
                                 <RefRO<Boar>, RefRO<LocalTransform>, RefRW<Health>>().WithEntityAccess())
                    {
                        var boarPosition = localTransform.ValueRO.Position;
                        if (math.distance(spearPosition, boarPosition) > SpearDamageRadius)
                        {
                            continue;
                        }

                        health.ValueRW.CurrentHealth -= SpearDamage;
                        if (health.ValueRO.CurrentHealth < 0)
                        {
                            SystemAPI.SetComponentEnabled<IsAlive>(boarEntity, false);
                        }
                        else
                        {
                            ecb.AddComponent(ecb.CreateEntity(), new DamageEvent
                            {
                                Position = spearPosition,
                                TargetType = UnitType.Boar
                            });
                            ecb.AddComponent(ecb.CreateEntity(), new SoundEvent
                            {
                                Position = spearPosition,
                                Type = SoundEventType.SpearHit
                            });
                        }

                        break;
                    }
                }
            }
        }
    }
}