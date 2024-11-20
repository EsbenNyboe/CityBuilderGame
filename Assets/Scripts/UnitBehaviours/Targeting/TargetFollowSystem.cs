using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetFollow : IComponentData
    {
        public Entity Target;
        public float DesiredRange;
        public float CurrentDistanceToTarget;
    }

    public partial struct TargetFollow
    {
        public readonly bool TryGetTarget(out Entity target)
        {
            target = Target;
            return target != Entity.Null;
        }
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct TargetFollowSystem : ISystem
    {
        private const bool IsDebugging = true;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            foreach (var (targetFollow, localTransform, pathFollow, entity) in
                     SystemAPI.Query<RefRW<TargetFollow>, RefRO<LocalTransform>, RefRO<PathFollow>>()
                         .WithEntityAccess())
            {
                if (!targetFollow.ValueRO.TryGetTarget(out var target))
                {
                    continue;
                }

                if (!transformLookup.HasComponent(target))
                {
                    DebugHelper.LogError("Target has no transform!");
                    continue;
                }

                var targetPosition = transformLookup[target].Position;
                var currentPosition = localTransform.ValueRO.Position;
                if (IsDebugging)
                {
                    Debug.DrawLine(currentPosition, targetPosition, Color.red);
                }

                // TODO: Make it able to update its path, while moving
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var distanceToTarget = math.distance(currentPosition, targetPosition);
                targetFollow.ValueRW.CurrentDistanceToTarget = distanceToTarget;

                if (distanceToTarget > targetFollow.ValueRO.DesiredRange)
                {
                    PathHelpers.TrySetPath(ecb, entity, currentPosition, targetPosition, IsDebugging);
                }
            }
        }
    }
}