using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class UnitMoveOrderSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        var mousePosition = UtilsClass.GetMouseWorldPosition();
        var cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();

        PathfindingGridSetup.Instance.pathfindingGrid.GetXY(mousePosition + new Vector3(1, 1) * cellSize * 0.5f, out var endX, out var endY);

        ValidateGridPosition(ref endX, ref endY);

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
        {
            PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);

            ValidateGridPosition(ref startX, ref startY);

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = new int2(endX, endY)
            });
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }
}