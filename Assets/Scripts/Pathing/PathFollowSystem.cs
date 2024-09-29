using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (localTransform, pathPositionBuffer, pathFollow, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                continue;
            }

            var pathPosition = pathPositionBuffer[pathFollow.ValueRO.PathIndex].Position;
            var targetPosition = new float3(pathPosition.x, pathPosition.y, 0);
            var moveDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);
            var moveSpeed = 5f;

            localTransform.ValueRW.Position += moveDirection * moveSpeed * SystemAPI.Time.DeltaTime;

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.1f)
            {
                // next waypoint
                pathFollow.ValueRW.PathIndex --;

                if (pathFollow.ValueRO.PathIndex < 0)
                {
                    GridSetup.Instance.OccupationGrid.GetXY(localTransform.ValueRO.Position, out var posX, out var posY);
                    if (GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).IsOccupied())
                    {
                        Debug.Log("OCCUPIED: " + posX + " " + posY);
                    }
                    else
                    {
                        GridSetup.Instance.OccupationGrid.GetGridObject(posX, posY).SetOccupied(entity);
                        Debug.Log("Set occupied: " + entity);
                    }
                }
            }
        }
    }
}