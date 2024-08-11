using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

/*
public class PathFollowSystem : ComponentSystem {

    protected override void OnUpdate() {
        Entities.ForEach((DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
            if (pathFollow.pathIndex >= 0) {
                int2 pathPosition = pathPositionBuffer[pathFollow.pathIndex].position;

                float3 targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
                float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
                float moveSpeed = 3f;

                translation.Value += moveDir * moveSpeed * Time.DeltaTime;

                if (math.distance(translation.Value, targetPosition) < .1f) {
                    // Next waypoint
                    pathFollow.pathIndex--;
                }
            }
        });
    }

}
*/