using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetSelectorSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (targetFollow, localTransform) in SystemAPI.Query<RefRW<TargetFollow>, RefRO<LocalTransform>>()
                         .WithAll<TargetSelector>())
            {
                if (TryGetClosestTarget(ref state, localTransform.ValueRO.Position, out var target))
                {
                    targetFollow.ValueRW.Target = target;
                }
            }
        }

        private bool TryGetClosestTarget(ref SystemState state, float3 position, out Entity target)
        {
            target = Entity.Null;
            var shortestTargetDistance = float.MaxValue;
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Targetable>()
                         .WithEntityAccess())
            {
                var targetPosition = localTransform.ValueRO.Position;
                var distance = math.distance(position, targetPosition);
                if (distance < shortestTargetDistance)
                {
                    shortestTargetDistance = distance;
                    target = entity;
                }
            }

            return target != Entity.Null;
        }
    }
}