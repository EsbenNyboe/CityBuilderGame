using Audio;
using Grid;
using SpriteTransformNS;
using UnitAgency.Data;
using UnitBehaviours.ActionGateNS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct IsThrowingSpear : IComponentData
    {
        public Entity Target;
    }

    public struct IsHoldingSpear : IComponentData
    {
    }

    public partial struct IsThrowingSpearSystem : ISystem
    {
        public static readonly float Range = 5f;
        private const float ThrowingSpearTime = 0.5f;
        private const float PostThrowWaitTime = 0.5f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();

            foreach (
                var (isThrowingSpear, actionGate, entity) in SystemAPI
                    .Query<RefRW<IsThrowingSpear>, RefRO<ActionGate>>()
                    .WithNone<IsHoldingSpear>()
                    .WithEntityAccess()
            )
            {
                if (
                    SystemAPI.Time.ElapsedTime
                    > actionGate.ValueRO.MinTimeOfAction + ThrowingSpearTime + PostThrowWaitTime
                )
                {
                    ecb.RemoveComponent<IsThrowingSpear>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            foreach (
                var (
                    isThrowingSpear,
                    localTransform,
                    spriteTransform,
                    actionGate,
                    entity
                    ) in SystemAPI
                    .Query<
                        RefRW<IsThrowingSpear>,
                        RefRO<LocalTransform>,
                        RefRW<SpriteTransform>,
                        RefRO<ActionGate>
                    >()
                    .WithEntityAccess()
                    .WithAll<IsHoldingSpear>()
            )
            {
                if (
                    isThrowingSpear.ValueRO.Target == Entity.Null
                    || !state.WorldUnmanaged.EntityManager.Exists(isThrowingSpear.ValueRO.Target)
                )
                {
                    ecb.RemoveComponent<IsHoldingSpear>(entity);
                    ecb.RemoveComponent<IsThrowingSpear>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var targetPosition = localTransformLookup[isThrowingSpear.ValueRO.Target].Position;
                var target = GridHelpers.GetXY(targetPosition);

                var attackDirection = ((Vector3)(targetPosition - position)).normalized;

                var angleInDegrees = attackDirection.x > 0 ? 0f : 180f;
                var spriteRotationOffset = quaternion.EulerZXY(
                    0,
                    math.PI / 180 * angleInDegrees,
                    0
                );
                spriteTransform.ValueRW.Rotation = spriteRotationOffset;

                if (
                    SystemAPI.Time.ElapsedTime
                    > actionGate.ValueRO.MinTimeOfAction + ThrowingSpearTime
                )
                {
                    ecb.RemoveComponent<IsHoldingSpear>(entity);
                    ecb.AddComponent(
                        ecb.CreateEntity(),
                        new Spear
                        {
                            Direction = new float2(attackDirection.x, attackDirection.y),
                            CurrentPosition = cell,
                            Target = target
                        }
                    );
                    ecb.AddComponent(
                        ecb.CreateEntity(),
                        new SoundEvent { Position = position, Type = SoundEventType.SpearThrow }
                    );
                }
            }
        }
    }
}