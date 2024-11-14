using UnitAgency;
using Unity.Entities;
using Unity.Transforms;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

[UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
public partial struct IsSleepingSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var sleepinessPerSecWhenSleeping = -0.2f * SystemAPI.Time.DeltaTime;
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        foreach (var (localTransform, pathFollow, moodSleepiness, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodSleepiness>>().WithAll<IsSleeping>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                ecb.RemoveComponent<IsSleeping>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                continue;
            }

            if (moodSleepiness.ValueRO.Sleepiness > 0)
            {
                moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenSleeping;
            }
            else
            {
                GoAwayFromBed(ref state, ecb, ref gridManager, entity);
            }
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }

    private void GoAwayFromBed(  ref SystemState state, EntityCommandBuffer commands, ref GridManager gridManager,
        Entity entity)
    {
        // I should leave the bed-area, so others can use the bed...
        var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;

        if (gridManager.EntityIsOccupant(unitPosition, entity))
        {
            gridManager.SetIsWalkable(unitPosition, true);
        }
        else
        {
            DebugHelper.Log("Seems like someone else was spooning me, while I slept... They can keep the bed!");
        }

        var currentCell = GridHelpers.GetXY(unitPosition);
        if (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out var nearbyCell))
        {
            PathHelpers.TrySetPath(commands, entity, currentCell, nearbyCell);
        }
    }
}