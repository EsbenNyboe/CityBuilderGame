using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoveToSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (localTransform, moveTo) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveTo>>())
        {
            if (moveTo.ValueRO.Move)
            {
                float reachedPositionDistance = 1f;
                if (math.distance(localTransform.ValueRO.Position, moveTo.ValueRO.Position) > reachedPositionDistance)
                {
                    float3 moveDir = math.normalize(moveTo.ValueRO.Position - localTransform.ValueRO.Position);
                    moveTo.ValueRW.LastMoveDir = moveDir;
                    localTransform.ValueRW.Position += moveDir * moveTo.ValueRO.MoveSpeed * SystemAPI.Time.DeltaTime;
                }
                else
                {
                    moveTo.ValueRW.Move = false;
                }
            }
        }
    }
}