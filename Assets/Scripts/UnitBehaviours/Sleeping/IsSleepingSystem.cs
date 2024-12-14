using Debugging;
using UnitAgency;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

public struct IsSleeping : IComponentData
{
    public bool IsInitialized;
}

[UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
public partial struct IsSleepingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridManager>();
        state.RequireForUpdate<DebugToggleManager>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();

        var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
        var isDebuggingOccupation = debugToggleManager.DebugBedOccupation;
        var isDebuggingPath = debugToggleManager.DebugPathfinding;
        var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var sleepinessPerSecWhenSleeping = -0.2f * SystemAPI.Time.DeltaTime;
        var gridManager = SystemAPI.GetSingleton<GridManager>();

        foreach (var (isSleeping,
                     localTransform,
                     pathFollow,
                     moodSleepiness,
                     spriteTransform,
                     entity) in SystemAPI
                     .Query<RefRW<IsSleeping>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodSleepiness>,
                         RefRW<SpriteTransform>>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                ecb.RemoveComponent<IsSleeping>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                continue;
            }

            spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, 0, 0);

            var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);

            if (!gridManager.EntityIsOccupant(currentCell, entity))
            {
                if (isDebuggingOccupation)
                {
                    Debug.LogError("I'm not the occupant! I shouldn't be sleeping here!");
                }
            }

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
                GoAwayFromBed(ref state, ecb, ref gridManager, entity, currentCell, isDebuggingOccupation,
                    isDebuggingSearch, isDebuggingPath);
            }
        }

        SystemAPI.SetSingleton(gridManager);
    }

    private void GoAwayFromBed(ref SystemState state, EntityCommandBuffer ecb, ref GridManager gridManager,
        Entity entity, int2 currentCell, bool isDebuggingOccupation, bool isDebuggingSearch, bool isDebuggingPath)
    {
        // I should leave the bed-area, so others can use the bed...
        if (gridManager.EntityIsOccupant(currentCell, entity))
        {
            gridManager.SetIsWalkable(currentCell, true);
        }
        else
        {
            if (isDebuggingOccupation)
            {
                ecb.AddComponent(ecb.CreateEntity(), new DebugPopupEvent
                {
                    Type = DebugPopupEventType.SleepOccupancyIssue,
                    Cell = currentCell
                });
                Debug.Log("Seems like someone else was spooning me, while I slept... Why does this happen?");
            }
        }

        if (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out var nearbyCell, isDebuggingSearch))
        {
            PathHelpers.TrySetPath(ecb, gridManager, entity, currentCell, nearbyCell, isDebuggingPath);
        }
    }
}