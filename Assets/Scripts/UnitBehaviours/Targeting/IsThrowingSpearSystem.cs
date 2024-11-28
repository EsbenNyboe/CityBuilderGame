using Audio;
using UnitAgency;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct IsThrowingSpear : IComponentData
    {
        public Entity Target;
        public float TimePassed;
    }

    public struct IsHoldingSpear : IComponentData
    {
    }

    public partial struct IsThrowingSpearSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        private const float ThrowingSpearTime = 0.5f;
        private const float PostThrowWaitTime = 0.5f;

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();

            foreach (var (isThrowingSpear, entity) in SystemAPI.Query<RefRW<IsThrowingSpear>>().WithNone<IsHoldingSpear>().WithEntityAccess())
            {
                isThrowingSpear.ValueRW.TimePassed += SystemAPI.Time.DeltaTime;
                if (isThrowingSpear.ValueRO.TimePassed > ThrowingSpearTime + PostThrowWaitTime)
                {
                    ecb.RemoveComponent<IsThrowingSpear>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            foreach (var (isThrowingSpear, localTransform, spriteTransform, entity) in SystemAPI
                         .Query<RefRW<IsThrowingSpear>, RefRO<LocalTransform>, RefRW<SpriteTransform>>().WithEntityAccess().WithAll<IsHoldingSpear>())
            {
                if (isThrowingSpear.ValueRO.Target == Entity.Null || !state.WorldUnmanaged.EntityManager.Exists(isThrowingSpear.ValueRO.Target))
                {
                    ecb.RemoveComponent<IsThrowingSpear>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                isThrowingSpear.ValueRW.TimePassed += SystemAPI.Time.DeltaTime;
                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var targetPosition = localTransformLookup[isThrowingSpear.ValueRO.Target].Position;
                var target = GridHelpers.GetXY(targetPosition);

                var attackDirection = ((Vector3)(targetPosition - position)).normalized;

                var angleInDegrees = attackDirection.x > 0 ? 0f : 180f;
                var spriteRotationOffset = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);
                spriteTransform.ValueRW.Rotation = spriteRotationOffset;

                if (isThrowingSpear.ValueRO.TimePassed > ThrowingSpearTime)
                {
                    ecb.RemoveComponent<IsHoldingSpear>(entity);
                    ecb.AddComponent(ecb.CreateEntity(), new Spear
                    {
                        Direction = new float2(attackDirection.x, attackDirection.y),
                        CurrentPosition = cell,
                        Target = target
                    });
                    ecb.AddComponent(ecb.CreateEntity(), new SoundEvent
                    {
                        Position = position,
                        Type = SoundEventType.SpearThrow
                    });
                }
            }
        }
    }
}