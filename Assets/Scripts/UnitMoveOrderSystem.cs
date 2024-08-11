using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class UnitMoveOrderSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent(entity, new PathfindingParams
                {
                    StartPosition = new int2(0, 0),
                    EndPosition = new int2(4, 0)
                });
            }

            entityCommandBuffer.Playback(EntityManager);
        }
    }
}