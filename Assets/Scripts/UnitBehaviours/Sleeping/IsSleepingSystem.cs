using UnitAgency;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

public partial struct IsSleepingSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var sleepinessPerSecWhenIdle = 0.02f * SystemAPI.Time.DeltaTime;
        var sleepinessPerSecWhenSleeping = -0.2f * SystemAPI.Time.DeltaTime;
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        foreach (var (localTransform, pathFollow, moodSleepiness, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodSleepiness>>().WithAll<IsSleeping>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }

            if (moodSleepiness.ValueRO.Sleepiness > 0)
            {
                moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenSleeping;
            }
            else
            {
                if (gridManager.IsInteractedWith(localTransform.ValueRO.Position))
                {
                    GoAwayFromBed(ref state, ecb, entity);
                }
                else
                {
                    ecb.RemoveComponent<IsSleeping>(entity);
                    ecb.AddComponent(entity, new IsDeciding());
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }

    private void GoAwayFromBed(ref SystemState state, EntityCommandBuffer commands, Entity entity)
    {
        // I should leave the bed-area, so others can use the bed...
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
        gridManager.SetIsWalkable(unitPosition, true);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

        GridHelpers.GetXY(unitPosition, out var x, out var y);

        // TODO: Make it search for distant cells too, in case all neighbours are non-walkable
        gridManager.GetSequencedNeighbourCell(x, y, out var neighbourX, out var neighbourY);
        commands.AddComponent(entity, new PathfindingParams
        {
            StartPosition = new int2(x, y),
            EndPosition = new int2(neighbourX, neighbourY)
        });
    }
}