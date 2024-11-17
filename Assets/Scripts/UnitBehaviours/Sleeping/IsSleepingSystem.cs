using UnitAgency;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

[UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
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
        state.CompleteDependency();

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var sleepinessPerSecWhenSleeping = -0.2f * SystemAPI.Time.DeltaTime;
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        foreach (var (isSleeping, localTransform, pathFollow, moodSleepiness, entity) in SystemAPI
                     .Query<RefRW<IsSleeping>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodSleepiness>>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                ecb.RemoveComponent<IsSleeping>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                continue;
            }

            var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);

            if (!isSleeping.ValueRO.IsInitialized)
            {
                isSleeping.ValueRW.IsInitialized = true;
                gridManager.SetIsWalkable(currentCell, false);
            }

            if (moodSleepiness.ValueRO.Sleepiness > 0)
            {
                moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenSleeping;
            }
            else
            {
                GoAwayFromBed(ref state, ecb, ref gridManager, entity, currentCell);
            }
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }

    private void GoAwayFromBed(ref SystemState state, EntityCommandBuffer commands, ref GridManager gridManager,
        Entity entity, int2 currentCell)
    {
        // I should leave the bed-area, so others can use the bed...
        if (gridManager.EntityIsOccupant(currentCell, entity))
        {
            gridManager.SetIsWalkable(currentCell, true);
        }
        else
        {
            Debug.Log("Seems like someone else was spooning me, while I slept... Why does this happen?");
            // HACK:
            gridManager.SetIsWalkable(currentCell, true);
        }

        if (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out var nearbyCell))
        {
            PathHelpers.TrySetPath(commands, entity, currentCell, nearbyCell);
        }
    }
}