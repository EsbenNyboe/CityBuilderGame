using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetFollow : IComponentData
    {
        public Entity Target;
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

    public partial struct TargetFollowSystem : ISystem
    {
        private const float MoveSpeed = 1f;

        public void OnUpdate(ref SystemState state)
        {
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            foreach (var (targetFollow, localTransform) in
                     SystemAPI.Query<RefRW<TargetFollow>, RefRW<LocalTransform>>())
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
                var direction = math.normalizesafe(targetPosition - currentPosition);
                currentPosition += direction * MoveSpeed * SystemAPI.Time.DeltaTime;
                localTransform.ValueRW.Position = currentPosition;
                targetFollow.ValueRW.CurrentDistanceToTarget = math.distance(currentPosition, targetPosition);

                Debug.DrawLine(currentPosition, targetPosition, Color.red);
            }
        }
    }
}