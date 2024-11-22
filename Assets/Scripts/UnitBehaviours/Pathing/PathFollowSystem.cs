using Debugging;
using UnitBehaviours;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;


public partial struct PathFollow : IComponentData
{
    public int PathIndex;
    public float MoveSpeedMultiplier;

    public PathFollow (int pathIndex, float moveSpeedMultiplier = 1)
    {
        PathIndex = pathIndex;
        MoveSpeedMultiplier = moveSpeedMultiplier;
    }
}

public partial struct PathFollow
{
    public readonly bool IsMoving()
    {
        return PathIndex >= 0;
    }
}

[UpdateInGroup(typeof(UnitStateSystemGroup))]
[BurstCompile]
public partial struct PathFollowSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;
    private const float BaseSpeed = 5f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SocialDynamicsManager>();
        state.RequireForUpdate<DebugToggleManager>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
        var isDebuggingPath = debugToggleManager.DebugPathfinding;
        var isDebuggingSearch = debugToggleManager.DebugPathSearchEmptyCells;
        var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (localTransform, pathPositionBuffer, pathFollow, spriteTransform, socialRelationships, entity) in
                 SystemAPI
                     .Query<RefRW<LocalTransform>, DynamicBuffer<PathPosition>, RefRW<PathFollow>,
                         RefRW<SpriteTransform>, RefRW<SocialRelationships>>().WithEntityAccess())
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

            var moveAmount = BaseSpeed * pathFollow.ValueRO.MoveSpeedMultiplier * SystemAPI.Time.DeltaTime;

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
                        socialRelationships,
                        socialDynamicsManager,
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
                Debug.DrawLine(currentPosition, new Vector3(pathEndPosition.x, pathEndPosition.y),
                    Color.red);
            }
        }
    }

    private void EndPathFollowing(ref SystemState state, EntityCommandBuffer ecb, Entity entity,
        RefRW<SocialRelationships> socialRelationships, SocialDynamicsManager socialDynamicsManager,
        float3 targetPosition,
        bool isDebuggingSearch, bool isDebuggingPath)
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        // TODO: If we implement path-invalidation, there's no need to check if the tile is walkable or not
        if (!gridManager.IsOccupied(targetPosition, entity) && gridManager.IsWalkable(targetPosition))
        {
            gridManager.SetOccupant(targetPosition, entity);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }
        else
        {
            if (gridManager.IsBed(targetPosition) && gridManager.IsOccupied(targetPosition, entity))
            {
                gridManager.TryGetOccupant(targetPosition, out var occupant);
                var fondnessOfBedOccupant = socialRelationships.ValueRO.Relationships[occupant];
                fondnessOfBedOccupant += socialDynamicsManager.ImpactOnBedBeingOccupied;
                socialRelationships.ValueRW.Relationships[occupant] = fondnessOfBedOccupant;
                if (fondnessOfBedOccupant < socialDynamicsManager.ThresholdForBecomingAnnoying)
                {
                    socialRelationships.ValueRW.AnnoyingDude = occupant;
                }
            }

            GridHelpers.GetXY(targetPosition, out var x, out var y);
            if (gridManager.TryGetNearbyEmptyCellSemiRandom(new int2(x, y), out var vacantCell, isDebuggingSearch))
            {
                PathHelpers.TrySetPath(ecb, entity, new int2(x, y), vacantCell, isDebuggingPath);
            }
            // TODO: Go to a nearby walkable-cell, then?
            else
            {
                if (isDebuggingPath)
                {
                    DebugHelper.LogError("NO NEARBY POSITION WAS FOUND FOR ENTITY: ", entity);
                }
            }
        }
    }
}