using Debugging;
using Grid;
using SpriteTransformNS;
using SystemGroups;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;

namespace UnitBehaviours.Pathing
{
    public partial struct PathFollow : IComponentData
    {
        public int PathIndex;
        public float MoveSpeedMultiplier;

        public PathFollow(int pathIndex, float moveSpeedMultiplier = 1)
        {
            PathIndex = pathIndex;
            MoveSpeedMultiplier = moveSpeedMultiplier;
        }
    }

    [UpdateInGroup(typeof(UnitStateSystemGroup))]
    public partial struct PathFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var isDebuggingPath = debugToggleManager.DebugPathfinding;
            var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (localTransform, pathPositionBuffer, pathFollow, spriteTransform, entity) in
                     SystemAPI
                         .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>,
                             RefRW<SpriteTransform>>().WithEntityAccess())
            {
                var pathIndex = pathFollow.ValueRO.PathIndex;
                if (pathIndex < 0)
                {
                    continue;
                }

                var currentPosition = localTransform.ValueRO.Position;
                var targetPosition = GridHelpers.GetWorldPosition(pathPositionBuffer[pathIndex].Position);
                var distanceToTarget = math.distance(currentPosition, targetPosition);
                var pathLength = pathPositionBuffer.Length;
                if (pathLength == pathIndex + 1 && pathLength > 2)
                {
                    // TODO: Move this logic to PathfindingSystem for better performance
                    var nextTargetPosition = GridHelpers.GetWorldPosition(pathPositionBuffer[pathIndex - 1].Position);
                    var distanceFromCurrentToNextTarget = math.distance(currentPosition, nextTargetPosition);
                    var distanceFromTargetToNextTarget = math.distance(targetPosition, nextTargetPosition);
                    if (distanceFromCurrentToNextTarget < distanceToTarget + distanceFromTargetToNextTarget)
                    {
                        targetPosition = nextTargetPosition;
                        distanceToTarget = math.distance(currentPosition, targetPosition);
                        pathIndex--;
                        pathFollow.ValueRW.PathIndex = pathIndex;
                    }
                }

                var moveAmount = unitBehaviourManager.MoveSpeed *
                                 pathFollow.ValueRO.MoveSpeedMultiplier *
                                 SystemAPI.Time.DeltaTime;

                while (distanceToTarget - moveAmount < 0)
                {
                    // I have overshot my target. Therefore, I'll increment my path-index.
                    pathIndex--;
                    pathFollow.ValueRW.PathIndex = pathIndex;
                    currentPosition = targetPosition;

                    if (pathIndex < 0)
                    {
                        // I have reached my destination.
                        moveAmount = 0;
                        distanceToTarget = 0;
                        EndPathFollowing(ref state, ecb, entity,
                            targetPosition,
                            isDebuggingSearch,
                            isDebuggingPath);
                    }
                    else
                    {
                        // I have not reached my destination.
                        // So I'll start targeting the next path-position in my buffer.
                        moveAmount -= distanceToTarget;
                        currentPosition = targetPosition;
                        targetPosition = GridHelpers.GetWorldPosition(pathPositionBuffer[pathIndex].Position);
                        distanceToTarget = math.distance(currentPosition, targetPosition);
                    }
                }

                var moveDirection = math.normalizesafe(targetPosition - currentPosition);

                // currentPosition is either my actual current position,
                // or the position of the last path-position, I traversed this frame.
                currentPosition += moveDirection * moveAmount;
                localTransform.ValueRW.Position = currentPosition;

                if (moveDirection.x != 0)
                {
                    var angleInDegrees = moveDirection.x > 0 ? 0f : 180f;
                    spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);
                }

                if (isDebuggingPath)
                {
                    var pathEndPosition = pathPositionBuffer[0].Position;
//                Debug.DrawLine(currentPosition, new Vector3(pathEndPosition.x, pathEndPosition.y),
//                    Color.red);
                }
            }
        }

        private void EndPathFollowing(ref SystemState state, EntityCommandBuffer ecb, Entity entity,
            float3 targetPosition,
            bool isDebuggingSearch, bool isDebuggingPath)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            // TODO: If we implement path-invalidation, there's no need to check if the tile is walkable or not
            if (!gridManager.IsOccupied(targetPosition, entity) && gridManager.IsWalkable(targetPosition))
            {
                gridManager.SetOccupant(targetPosition, entity);
                SystemAPI.SetSingleton(gridManager);
            }
            else
            {
                var cell = GridHelpers.GetXY(targetPosition);
                var foundPath = false;
                if (gridManager.TryGetNearbyEmptyCellSemiRandom(cell, out var newTarget, isDebuggingSearch))
                {
                    foundPath = PathHelpers.TrySetPath(ecb, gridManager, entity, cell, newTarget, isDebuggingPath);
                }
                else if (gridManager.TryGetClosestWalkableCell(cell, out newTarget, false, false))
                {
                    foundPath = PathHelpers.TrySetPath(ecb, gridManager, entity, cell, newTarget, isDebuggingPath);
                }

                if (newTarget.x < 0)
                {
                    Debug.LogError("No nearby walkable cell found");
                }
                else if (!foundPath)
                {
                    // Defy physics, and move to new target
                    if (gridManager.TryGetClosestWalkableCell(cell, out newTarget, false))
                    {
                        PathHelpers.SetPath(ecb, entity, cell, newTarget, true);
                    }
                    else
                    {
                        Debug.LogError("No nearby walkable cell found");
                    }
                }
            }
        }
    }
}