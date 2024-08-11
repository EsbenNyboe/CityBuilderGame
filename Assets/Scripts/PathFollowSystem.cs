using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (localTransform, pathPositionBuffer, pathFollow) in SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>>())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                continue;
            }

            var pathPosition = pathPositionBuffer[pathFollow.ValueRO.PathIndex].Position;
            var targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
            var moveDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);
            var moveSpeed = 3f;

            localTransform.ValueRW.Position += moveDirection * moveSpeed * SystemAPI.Time.DeltaTime;

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.1f)
            {
                // next waypoint
                pathFollow.ValueRW.PathIndex --;
            }
        }
    }
}