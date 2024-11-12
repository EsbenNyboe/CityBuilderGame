using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetFollowSystem : ISystem
    {
        private const float MoveSpeed = 5f;

        public void OnUpdate(ref SystemState state)
        {
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            foreach (var (targetFollow, localTransform) in
                     SystemAPI.Query<RefRW<TargetFollow>, RefRW<LocalTransform>>())
            {
                var target = targetFollow.ValueRO.Target;
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
            }
        }
    }
}