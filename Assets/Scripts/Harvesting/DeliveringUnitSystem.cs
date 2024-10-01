using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class DeliveringUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        // TODO: Check if WithAll is necessary
        foreach (var (localTransform, deliveringUnit, pathFollow, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<DeliveringUnit>, RefRO<PathFollow>>()
                     .WithAll<DeliveringUnit>().WithEntityAccess())
        {
            GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
            var unitIsTryingToDeliver = pathFollow.ValueRO.PathIndex < 0;
            if (!unitIsTryingToDeliver)
            {
                continue;
            }

            if (EntityManager.HasComponent<PathfindingParams>(entity))
            {
                continue;
            }

            //if (startX != 5 && startY != 5)
            //{
            //    continue;
            //}

            // TODO: Insert check for proximity to dropdown

            var harvestTarget = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
            PathingHelpers.ValidateGridPosition(ref startX, ref startY);

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = harvestTarget
            });
            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
            EntityManager.SetComponentEnabled<DeliveringUnit>(entity, false);
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}
