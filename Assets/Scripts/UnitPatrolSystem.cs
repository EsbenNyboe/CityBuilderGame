using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class UnitPatrolSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // foreach (var (unitPatrol, pathFollow) in SystemAPI.Query<RefRW<UnitPatrol>, RefRO<PathFollow>>())
        // {
        //     if (pathFollow.ValueRO.PathIndex < 0)
        //     {
        //         if (unitPatrol.ValueRO.IsPatrolling)
        //         {
        //         }
        //
        //         unitPatrol.ValueRW.IsPatrolling = false;
        //     }
        //     else
        //     {
        //         unitPatrol.ValueRW.IsPatrolling = true;
        //     }
        // }

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (unitPatrol, pathFollow, localTransform, entity) in SystemAPI
                     .Query<RefRW<UnitPatrol>, RefRO<PathFollow>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                if (unitPatrol.ValueRO.IsPatrolling)
                {
                    unitPatrol.ValueRW.IsPatrolling = false;
                    PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);

                    ValidateGridPosition(ref startX, ref startY);

                    GetRandomPosition(out var endX, out var endY);

                    ValidateWalkableTargetPosition(ref endX, ref endY);

                    entityCommandBuffer.AddComponent(entity, new PathfindingParams
                    {
                        StartPosition = new int2(startX, startY),
                        EndPosition = new int2(endX, endY)
                    });
                }
            }
            else
            {
                unitPatrol.ValueRW.IsPatrolling = true;
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void ValidateWalkableTargetPosition(ref int endX, ref int endY)
    {
        var maxAttempts = 10;
        var currentAttempt = 0;
        while (currentAttempt < maxAttempts)
        {
            currentAttempt++;

            if (PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(endX, endY).IsWalkable())
            {
                return;
            }

            GetRandomPosition(out endX, out endY);
        }

        Debug.Log("Could not find walkable path");
    }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

    private void GetRandomPosition(out int x, out int y)
    {
        x = Random.Range(0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1) ;
        y = Random.Range(0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1) ;
    }
}