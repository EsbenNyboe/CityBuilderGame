using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitAgency
{
    internal partial struct UnitAgencySystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var ecbSystemSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var commands = ecbSystemSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<IsDecidingTag>>().WithEntityAccess())
            {
                commands.RemoveComponent<IsDecidingTag>(entity);
                DecideNextBehaviour(ref state, commands, entity);
            }
        }

        private void DecideNextBehaviour(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            var moodSleepiness = SystemAPI.GetComponent<MoodSleepiness>(entity);
            var isSleeping = SystemAPI.HasComponent<IsSleeping>(entity);
            var isSeekingBed = SystemAPI.HasComponent<IsSeekingBed>(entity);
            var isWellRested = moodSleepiness.Sleepiness <= 0;
            var isMortallyTired = moodSleepiness.Sleepiness >= 1f;
            var isTired = !isMortallyTired && moodSleepiness.Sleepiness > 0.1f;

            if (isSleeping)
            {
                if (isWellRested)
                {
                    WakeUp(ref state, commands, entity);
                }
            }
            else
            {
                if (isTired)
                {
                    if (isSeekingBed)
                    {
                        Debug.Log("Try sleep");
                        TryGoToSleep(ref state, commands, entity);
                    }
                    else
                    {
                        SeekBed(ref state, commands, entity);
                    }
                }
                else if (isMortallyTired)
                {
                    // TOO TIRED! DEATH!
                    commands.DestroyEntity(entity);
                }
            }
        }

        private void WakeUp(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            // Not tired anymore. Time to get out of bed!
            commands.RemoveComponent<IsSleeping>(entity);

            // I should leave the bed-area, so others can use the bed...
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            GridHelpers.GetXY(unitPosition, out var x, out var y);
            gridManager.GetSequencedNeighbourCell(x, y, out var neighbourX, out var neighbourY);
            commands.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(x, y),
                EndPosition = new int2(neighbourX, neighbourY)
            });
        }

        private void TryGoToSleep(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            if (gridManager.IsInteractable(unitPosition))
            {
                // Ahhhh, now I can rest...
                commands.RemoveComponent<IsSeekingBed>(entity);
                commands.AddComponent<IsSleeping>(entity);
            }
            else
            {
                // WTF?? Where's the bed?? Gotta look for a new one then..
                SeekBed(ref state, commands, entity);
            }
        }

        private void SeekBed(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            // Tired... must find bed...
            commands.AddComponent<IsSeekingBed>(entity);

            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            var closestBed = new float3(-1, -1, -1);
            var shortestDistance = Mathf.Infinity;
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            foreach (var (bed, localTransform) in SystemAPI.Query<RefRO<Bed>, RefRO<LocalTransform>>())
            {
                var bedPosition = localTransform.ValueRO.Position;
                var distance = Vector3.Distance(unitPosition, bedPosition);
                if (distance < shortestDistance && !gridManager.IsInteractedWith(bedPosition))
                {
                    shortestDistance = distance;
                    closestBed = bedPosition;
                }
            }

            if (closestBed.x < 0)
            {
                // We don't have any beds!!! I can't live in this world anymore! Goodbye!
                commands.DestroyEntity(entity);
            }
            else
            {
                // I found a bed!! I will go there! 
                GridHelpers.GetXY(unitPosition, out var startX, out var startY);
                GridHelpers.GetXY(closestBed, out var endX, out var endY);
                commands.AddComponent(entity, new PathfindingParams
                {
                    StartPosition = new int2(startX, startY),
                    EndPosition = new int2(endX, endY)
                });
            }
        }
    }
}