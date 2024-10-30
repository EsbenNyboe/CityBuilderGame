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
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            GridHelpers.GetXY(unitPosition, out var x, out var y);
            var moodSleepiness = SystemAPI.GetComponent<MoodSleepiness>(entity);
            var isSleeping = SystemAPI.HasComponent<IsSleeping>(entity);
            var isSeekingBed = SystemAPI.HasComponent<IsSeekingBed>(entity);
            var isWellRested = moodSleepiness.Sleepiness <= 0;
            var isMortallyTired = moodSleepiness.Sleepiness >= 1f;
            var isTired = !isMortallyTired && moodSleepiness.Sleepiness > 0.1f;
            var isStaying = true;
            var isDead = false;
            Debug.Log("Decision pos: " + x + " " + y);

            if (gridManager.IsOccupied(unitPosition, entity))
            {
                isStaying = false;

                if (!gridManager.TryGetNearbyVacantCell(x, y, out var vacantCell))
                {
                    BurstDebugHelpers.DebugLogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: ", entity);
                    isDead = true;
                    commands.DestroyEntity(entity);
                }

                commands.AddComponent(entity, new PathfindingParams
                {
                    StartPosition = new int2(x, y),
                    EndPosition = vacantCell
                });
            }
            else if (isSleeping)
            {
                if (isWellRested)
                {
                    isStaying = false;
                    WakeUp(ref state, commands, entity);
                }
            }
            else // if (!isSleeping)
            {
                if (isTired)
                {
                    if (isSeekingBed)
                    {
                        if (!TryGoToSleep(ref state, commands, entity))
                        {
                            // WTF?? Where's the bed?? Gotta look for a new one then..
                            isStaying = false;
                            if (!SeekBed(ref state, commands, entity))
                            {
                                // We don't have any beds!!! I can't live in this world anymore! Goodbye!
                                isDead = true;
                                commands.DestroyEntity(entity);
                            }
                        }
                    }
                    else
                    {
                        isStaying = false;
                        if (!SeekBed(ref state, commands, entity))
                        {
                            // We don't have any beds!!! I can't live in this world anymore! Goodbye!
                            isDead = true;
                            commands.DestroyEntity(entity);
                        }
                    }
                }
                else if (isMortallyTired)
                {
                    // TOO TIRED! DEATH!
                    isDead = true;
                    commands.DestroyEntity(entity);
                }
            }

            if (isStaying)
            {
                gridManager.SetOccupant(unitPosition, entity);
            }
            else
            {
                gridManager.TryClearOccupant(unitPosition, entity);
                gridManager.TryClearInteractor(unitPosition, entity);
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        private void WakeUp(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            // Not tired anymore. Time to get out of bed!
            commands.RemoveComponent<IsSleeping>(entity);

            // I should leave the bed-area, so others can use the bed...
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            GridHelpers.GetXY(unitPosition, out var x, out var y);

            // TODO: Make it search for distant cells too, in case all neighbours are non-walkable
            gridManager.GetSequencedNeighbourCell(x, y, out var neighbourX, out var neighbourY);
            commands.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(x, y),
                EndPosition = new int2(neighbourX, neighbourY)
            });
        }

        private bool TryGoToSleep(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;

            if (!gridManager.IsInteractable(unitPosition))
            {
                return false;
            }

            // Ahhhh, now I can rest...
            commands.RemoveComponent<IsSeekingBed>(entity);
            commands.AddComponent<IsSleeping>(entity);
            gridManager.SetInteractor(unitPosition, entity);
            gridManager.SetIsWalkable(unitPosition, false);
            return true;
        }

        private bool SeekBed(ref SystemState state, EntityCommandBuffer commands, Entity entity)
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
                return false;
            }

            // I found a bed!! I will go there! 
            GridHelpers.GetXY(unitPosition, out var startX, out var startY);
            GridHelpers.GetXY(closestBed, out var endX, out var endY);
            commands.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = new int2(endX, endY)
            });
            return true;
        }
    }
}